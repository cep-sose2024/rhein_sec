## Executing the Docker Container

To run the Docker container, use the following command:

```bash
sudo docker run --name vault_test \
    -v /home/phillip/config/vaultConfig:/vault/config/vault.hcl \
    -v path/to/your/certs:/vault/file/ \
    -p 8202:8202 \
    --cap-add IPC_LOCK \
    hashicorp/vault:latest server
```

### Configuration File

The configuration file should be as follows:

```hcl
ui = true
disable_mlock = true
log_level = "info"

storage "raft" {
  path    = "/vault/file"
  node_id = "vault_node_1"
}

listener "tcp" {
  address     = "0.0.0.0:8202"
  tls_disable = "false"
  tls_cert_file = "/vault/file/cert.pem"
  tls_key_file = "/vault/file/key.pem"
}

api_addr     = "https://127.0.0.1:8202"
cluster_addr = "https://127.0.0.1:8201"
max_lease_ttl = "8760h"
```

### Running the RheinSec Docker Container

To run the RheinSec Docker container, use the following command:

```bash
docker run --name server -p 5000:5000 -v /path/to/your/certs/:/app/certs -v /path/to/your/config/nksconfig.json:/app/nksconfig.json rheinsec/network-key-storage:latest <your arguments>
```

### Building the Docker Image

To build the Docker image, use the following command:

```bash
docker build -t rheinsec/network-key-storage:latest .
```

in the same directory as the Dockerfile.

### Optional Arguments

The backend can be run with the following optional arguments:

```bash
./backend <--UseSwagger> <-o <your_log_file>> <<-port> > <-insecure>
```