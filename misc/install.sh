#!/bin/bash

# Get the port number from the script arguments, default to 8200 if not provided
PORT=${1:-8200}
# Check Linux distribution
echo "Checking Linux distribution..."
distro=$(awk -F= '/^NAME/{print $2}' /etc/os-release)

# Install Vault
echo "Installing Vault..."
if ! command -v curl &> /dev/null; then
    echo "curl could not be found"
    echo "Installing curl..."
    if command -v apt-get &> /dev/null; then
        sudo apt-get update && sudo apt-get install -y curl
    elif command -v dnf &> /dev/null; then
        sudo dnf install -y curl
    elif command -v pacman &> /dev/null; then
        sudo pacman -S curl
    else
        echo "Unsupported package manager. Please install curl manually and try again!"
        exit 1
    fi
fi

if ! command -v dotnet &> /dev/null; then
    echo ".NET runtime could not be found"
    echo "Installing .NET runtime..."
    if command -v apt-get &> /dev/null; then
               sudo apt-get install dotnet8
    elif command -v dnf &> /dev/null; then
        sudo dnf install -y dotnet-sdk-8.0
    elif command -v pacman &> /dev/null; then
        sudo pacman -S dotnet-sdk-8.0
    else
        echo "Unsupported package manager. Please install .NET runtime manually and try again!"
        exit 1
    fi
fi

if ! command -v vault &> /dev/null; then
    echo "Vault could not be found"
    echo "Installing Vault..."
    if command -v apt-get &> /dev/null; then
        curl -fsSL https://apt.releases.hashicorp.com/gpg | sudo apt-key add -
        echo "deb [arch=amd64] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
        sudo apt-get update && sudo apt-get install -y vault
        if [ $? -ne 0 ]; then
            echo "Failed to install Vault from HashiCorp's repository. Trying manual installation..."
            VAULT_VERSION="1.16.2"
            wget https://releases.hashicorp.com/vault/${VAULT_VERSION}/vault_${VAULT_VERSION}_linux_amd64.zip
            unzip vault_${VAULT_VERSION}_linux_amd64.zip
            sudo mv vault /usr/local/bin/
        fi
    elif command -v dnf &> /dev/null; then
        sudo dnf install -y vault
    elif command -v pacman &> /dev/null; then
        sudo pacman -S vault
    else
        echo "Unsupported package manager. Please install Vault manually and try again!"
    fi
else
    echo "Vault is already installed"
fi


# Create a folder called 'vault'
echo "Creating 'vault' directory..."
mkdir vault # Create a new directory named 'vault'

# Create a 'certs' folder inside 'vault'
echo "Creating 'certs' directory inside 'vault'..."
mkdir vault/certs # Create a new directory named 'certs' inside 'vault'

# Create the 'VaultConfig' file with the provided content
echo "Creating 'VaultConfig'..."
cat << EOF > vault/VaultConfig # Create a new file named 'VaultConfig' with the provided content
ui = false
disable_mlock = true
log_level = "info"

storage "raft" {
  path    = "./vault/data"
  node_id = "node1"
}

listener "tcp" {
  address     = "0.0.0.0:$PORT"
  tls_disable = "false"
  tls_skip_verify = "true"
  tls_cert_file = "$(pwd)/vault/certs/cert.crt"
  tls_key_file = "$(pwd)/vault/certs/key.pem"
}

api_addr     = "https://127.0.0.1:$PORT"
cluster_addr = "https://127.0.0.1:8201"
max_lease_ttl = "8760h"
EOF

# Create certificates for your server using OpenSSL
RED='\033[0;31m'
NO_COLOR='\033[0m'

echo -e "$Creating certificates..."
openssl genpkey -algorithm RSA -out vault/certs/key.pem -pkeyopt rsa_keygen_bits:2048 # Generate a private key
openssl req -new -x509 -key vault/certs/key.pem -out vault/certs/cert.crt -days 365 \
-subj "/C=AU/ST=Some-State/O=Internet Widgits Pty Ltd/CN=localhost" \
-addext "subjectAltName = DNS:localhost, IP:127.0.0.1"

echo -e "${RED}Trusting the certificate...${NO_COLOR}"

cert_name=$(sha256sum vault/certs/cert.crt | awk '{print $1}')

if [ -d "/usr/local/share/ca-certificates/" ]; then
    sudo cp vault/certs/cert.crt /usr/local/share/ca-certificates/$cert_name.crt # Copy the certificate to trusted CA directory
    sudo update-ca-certificates # Update the CA certificate bundle
elif [ -d "/etc/ca-certificates/trust-source/anchors/" ]; then
    sudo cp vault/certs/cert.crt /etc/ca-certificates/trust-source/anchors/$cert_name.crt # Copy the certificate to trusted CA directory
    sudo trust extract-compat # Update the CA certificate bundle
else
    echo -e "${RED}Unable to find the directory to store trusted CA certificates. Please check your distribution's documentation.${NO_COLOR}"
fi



# Start the Vault server
echo "Starting Vault server..."
mkdir -p vault/data/ && touch vault/data/vault.db # Create the necessary directories and files for Vault
nohup vault server -config=vault/VaultConfig > vaultoutput.txt 2>&1 & # Start the Vault server in the background and redirect its output to 'vaultoutput.txt'

# Wait for Vault to start
sleep 10 # Wait for 10 seconds to ensure that Vault has started

# Export Vault address
export VAULT_ADDR="https://localhost:$PORT" # Set the VAULT_ADDR environment variable to the address of the Vault server

# Skip TLS verification
export VAULT_SKIP_VERIFY=true # Set the VAULT_SKIP_VERIFY environment variable to true to skip TLS verification

# Initialize Vault server and save the keys and root token to a file
echo "Initializing Vault server..."
vault operator init > unsealKeys.txt # Initialize the Vault server and save the unseal keys and root token to 'unsealKeys.txt'

# Create the 'unsealVault.sh' script
echo "Creating 'unsealVault.sh'..."
cat << EOF > unsealVault.sh # Create a new script named 'unsealVault.sh' that unseals the Vault
#!/bin/bash

# Get the port number from the script arguments, default to $PORT if not provided
PORT=\${1:-$PORT}

# Export Vault address
export VAULT_ADDR="https://localhost:\$PORT"

# Skip TLS verification
export VAULT_SKIP_VERIFY=true

# Read the unseal keys from 'unsealKeys.txt' and unseal the Vault
for KEY in \$(grep 'Unseal Key' unsealKeys.txt | awk '{print \$4}'); do
  vault operator unseal \$KEY
done
EOF

# Make the 'unsealVault.sh' script executable
chmod +x unsealVault.sh # Make 'unsealVault.sh' executable

releases=$(curl -s "https://api.github.com/repos/cep-sose2024/rhein_sec/releases")

# Find the latest release that starts with "backend"
latest_backend_release=$(echo "$releases" | grep -o 'https://github.com/cep-sose2024/rhein_sec/releases/download/backend[^"]*' | head -n 1)

# Download the latest backend server version
echo "Downloading the latest backend server version..."
curl -LJO "$latest_backend_release"

# Extract the contents of the downloaded file
filename=$(basename "$latest_backend_release")
echo "Extracting the contents of the downloaded file..."
tar -xzf "$filename"

# Create the 'nksconfig.json' file in the 'backend_release' folder
echo "Creating 'nksconfig.json'..."
ROOT_TOKEN=$(grep 'Initial Root Token' unsealKeys.txt | awk '{print $4}') # Get the root token from 'unsealKeys.txt'
cat << EOF > backend_release/nksconfig.json # Create a new file named 'nksconfig.json' with the provided content
[
    {
      "address": "$VAULT_ADDR",
      "token": "$ROOT_TOKEN"
    }
]
EOF

# Unseal the Vault
./unsealVault.sh $PORT # Unseal the Vault using the 'unsealVault.sh' script

# Start the executable server with the --UseSwagger argument
echo "Starting the executable server..."
cd backend_release
nohup ./backend --UseSwagger > backendoutput.txt 2>&1 & # Start the executable server in the background and redirect its output to 'backendoutput.txt'
