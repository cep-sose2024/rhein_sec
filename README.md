<!-- markdown project template used: https://github.com/othneildrew/Best-README-Template -->
<a name="readme-top"></a>

<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

<br />
<div align="center">
  <a href="https://github.com/cep-sose2024/rhein_sec">
    <img src="static/img/logo_white.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">First of a Kind Secure Network Key Management Solution</h3>

<p align="center">
  A new network key storage solution for <a href="https://github.com/nmshd">Enmeshed</a>
    <br />
  </p>
</div>

---

<!-- ABOUT THE PROJECT -->
## 🔍 About The Project

### What is Enmeshed?
[Enmeshed](https://github.com/nmshd) is an open-source project by j&s Soft. The project offers a secure and futuristic concept for exchanging information or documents between people or organizations. The application uses end-to-end encryption for secure transfer and privacy. More information can be found [here](https://enmeshed.eu/explore/how_does_enmeshed_work).

### How does our Network Key Storage Solution work?
This application is designed to provide a secure method for storing keys, which are currently stored insecurely, to a network-based solution. This solution will be built on Hashicorp Vault and a C# ASP .NET core outward-facing API. The API will initialize user tokens and communicate with the Vault server.<br>
The backend of the application should be capable of running securely with any similar application that wishes to store keys securely and retrieve them using a user token.

### Why Network Key storage? 
Network key storage is designed to solve the issue of some devices not having a Hardware Security Module that is compatible with the Enmeshed application. This would still allow older, non-bleeding-edge devices to store their keys securely.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 👷‍♂️ Built With

* ASP .NET core 8.0.
* Hashicorp Vault 1.16.2.
* [Rust](https://github.com/cep-sose2024/rheinsec_rust-crypto) for the client side of the application.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---
## 🏃 Getting Started

### Installation Script

We provide a Linux installation script available at `misc/install.sh`. This script accepts four optional arguments:

1. The IP address for the Vault server (default is localhost)
2. The port number for the Vault server (default is 8200)
3. A flag to initialize the Vault server (default is true)
4. A flag to download the backend (default is true)

Ensure that you make the script executable before running it with the following command:

```bash
chmod +x install.sh
```

Then, execute it using:

```bash
./install.sh <your_desired_ip> <your_desired_vault_port> <initialize_vault> <download_backend>
```

Replace `<your_desired_ip>`, `<your_desired_vault_port>`, `<initialize_vault>`, and `<download_backend>` with your desired IP address, port number, initialization flag, and backend download flag, respectively. If not provided, the script will use the default values.
This script automatically installs all the required packages and launches the backend server. Please replace `<your_desired_vault_port>` with the specific port number you wish to use for the vault.

Every time you would like to unseal Vault, just run the unseal script in the same way with:
``` bash
./unsealVault.sh <your_desired_ip> <your_desired_vault_port>
```
But first, make sure that the server is started with:
``` bash
vault server -config=vault/VaultConfig
 ```
### Cluster Setup Script

We provide a Linux cluster setup script available at `misc/cluster.sh`. This script uses the installation script to create three Vault instances that form a cluster. Ensure that you make the script executable before running it with the following command:

```bash
chmod +x cluster.sh
```

Then, execute it using:

```bash
./cluster.sh
```
This would also download the server executable from the releases and configure the nksConfig.json file automatically, it would then run on http://localhost:5000.

To start the vaults, use the following command:

```bash
./cluster.sh start
```

This command will start the vault servers in `vault1`, `vault2`, and `vault3` directories and unseal them.

To stop the vaults, use the following command:

```bash
./cluster.sh stop
```

### Setting up the code
We recommend the following:
* JetBrains RustRover for the client-side code
* JetBrains Rider for the server-side code

### Or use one of the releases

### Setting up Vault
For testing purposes, we recommend using a locally hosted instance of Vault. Vault is available as a package on most Linux package managers.<br>
To install Vault on Arch using Pacman, use the following command:
``` bash 
sudo pacman -S vault 
```
Next, create a folder called `vault`, and download or copy the `VaultConfig` file from the misc folder.

Then, create certificates for your server using OpenSSL with the following commands:
```bash 
openssl genpkey -algorithm RSA -out key.pem -pkeyopt rsa_keygen_bits:2048

openssl req -new -x509 -key key.pem -out cert.pem -days 365
```
In the `VaultConfig` file, change the path to the certificates with the one for the key and cert that you just created.

Finally, execute the following commands to start the Vault server:
``` bash 
mkdir -p vault/data/ && touch vault/data/vault.db
vault server -config=VaultConfig
```
After that, go to https://localhost:8200/ui/ and set up the root token and unseal the vault.

Finally, put the root token in a file called `nksconfig.json` in the **backend/** folder. The file should look like this: 
```json
{
  "vaults": [
    {
      "address": "https://your_vault_address",
      "token": "hvs.your_root_token"
    }
  ],
  "token_refresh": 60
}
```
This configuration allows the RheinSec NKS solution to manage multiple instances of Vault hosting the secrets, which enhances the security of the user's secrets.

However, using this is not recommended. We suggest using multiple vault instances in a cluster for better security and redundancy.

The `token_refresh` variable specifies the lifespan of the tokens (in seconds) before they are replaced with new ones.

### Using the Backend Server
Assuming you have already downloaded the backend server executable from the releases and configured the `nksConfig.json` file, you can run it via:
```
./backend <--UseSwagger> -o <your_log_file> -port <port_number> -insecure -http
```

[Backend README](backend/backend/README.md)

#### Known Issues:
- The C# code could produce errors if the certificate isn't trusted by your local CA.

  To fix this issue, add the certificate to your local trusted certificates. On Linux, this is typically located in `/usr/local/share/ca-certificates/`. Alternatively, you can run the server with the `-insecure` argument which would make it ignore Certificate errors.


### Testing the code with the Crypto Abstraction Layer 

You can test it using our own [fork](https://github.com/cep-sose2024/rhein_sec) of the Crypto Abstraction Layer. For more information, refer to the README in that repository.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---
## ‍🛡️️ Security Configuration

There is no single correct configuration for our NKS solution due to the flexibility of the tools we provide. However, here are some general guidelines and best practices to follow:

1. All Vault instances should be behind a proxy and accessible only from other Vault instances and the C# backend server.
    1. The clustering port should be open to other Vault instances.
    2. The API port should be open to the C# backend server.
2. It is recommended to use Docker or Kubernetes to manage the Vault instances.
3. Use the C# server-side wrapper Docker Container. More details can be found here: [Backend README](backend/backend/README.md)

    1. Next, limit the outbound traffic of the container to just the Vault instances.
    2. To do this, we recommend using `iptables` to limit the traffic to the Vault instances and the nginx server.
```bash
# Flush the DOCKER-USER chain (or create it if it does not exist)
sudo iptables -F DOCKER-USER

# Allow incoming and outgoing traffic to and from 172.17.0.1 on port 5000, this is your host IP in the Docker network 
sudo iptables -A DOCKER-USER -d 172.17.0.1 -p tcp --dport 5000 -j ACCEPT
sudo iptables -A DOCKER-USER -s 172.17.0.1 -p tcp --sport 5000 -j ACCEPT

# Allow incoming and outgoing traffic to and from your_vault_ip on port 5000
sudo iptables -A DOCKER-USER -d your_vault_ip -p tcp --dport 5000 -j ACCEPT
sudo iptables -A DOCKER-USER -s your_vault_ip -p tcp --sport 5000 -j ACCEPT

# Drop all other traffic to and from port 5000
sudo iptables -A DOCKER-USER -p tcp --dport 5000 -j DROP
sudo iptables -A DOCKER-USER -p tcp --sport 5000 -j DROP
```
**Please make sure to replace these IPs with the addresses of your actual vault instances.**
<p align="right">(<a href="#readme-top">back to top</a>)</p>

---






[contributors-shield]: https://img.shields.io/github/contributors/cep-sose2024/rhein_sec.svg?style=for-the-badge
[contributors-url]: https://github.com/cep-sose2024/rhein_sec/graphs/contributors
[stars-shield]: https://img.shields.io/github/stars/cep-sose2024/rhein_sec.svg?style=for-the-badge
[stars-url]: https://github.com/cep-sose2024/rhein_sec/stargazers
[issues-shield]: https://img.shields.io/github/issues/cep-sose2024/rhein_sec.svg?style=for-the-badge
[issues-url]: https://github.com/cep-sose2024/rhein_sec/issues
[license-shield]: https://img.shields.io/github/license/cep-sose2024/rhein_sec.svg?style=for-the-badge
[license-url]: https://github.com/cep-sose2024/rhein_sec/blob/master/LICENSE

