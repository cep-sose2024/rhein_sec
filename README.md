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
<a href="https://github.com/nmshd">Enmeshed</a> is a open source Project by j&s-soft, the project offeres a secure and feutreistic concept to extchange information or documents between People or organisations, the application uses end to end encryption for secure transfer and privacy, more information can be found [here](https://enmeshed.eu/explore/how_does_enmeshed_work)

### How does our Network Key storage Solution work?
This application is designed to provide a secure method for storing keys, which are currently stored insecurely to a network-based solution. This solution will be built on Hashicorp Vault and a C# ASP .NET core outward-facing API. The API will initialize user tokens and communicate with the Vault server.<br>
The backend of the application should be capable of running securely with any similar application that wishes to store keys securely and retrieve them using a user token.

The client side of the application is expected to communicate with the so-called ‘Crypto Abstraction Layer’, which is yet to be released by j&s-soft.


### Why Network Key storage? 
Network key storage is designed to solve the issue of some devices not having a Hardware Security Module that is compatible with the Enmeshed application. This would still allow older, non-bleeding-edge devices to store their keys securely.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 👷‍♂️ Built With

* ASP .NET core 8.0.
* Hashicorp Vault 1.16.2.
* Rust for the Client side of the application.

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
This script automatically installs all the required packages and launches the backend server, Please replace `<your_desired_vault_port>` with the specific port number you wish to use for the vault.

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
This would also make Download the server executable from the releases and configure the nksConfig.json file automatically, it would then run on http://localhost:5000.

To start the vaults, use the following command:

```bash
./cluster.sh start
```

This command will start the vault servers in `vault1`, `vault2`, and `vault3` directories and unseal them.

To stop the vaults, use the following command:

```bash
./cluster.sh stop
```

### setting up the code
We recommend the following:
* JetBrains RustRover for the client-side code
* JetBrains Rider for the server-side code

### or use one of the releases

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
``` json
[
    {
      "address": "https://localhost:8200",
      "token": "hvs.yourRootToken"
    },
    {
      "address": "https://localhost:8202",
      "token": "hvs.yourRootToken"
    }
]
```
This enables the RheinSec NKS solution to have many different instances of Vault hosting the secrets. This would make it very hard for the user’s secrets to get lost.


### Using the Client & the backend Server
The client is currently distributed as a Rust executable for Windows, Linux, and macOS. The latest releases can be found on the releases page.

The current capabilities are limited since the crypto layer hasn’t been implemented yet by the enmeshed developers, thus basic PoC code is implemented. There are two main methods:<br>
 one for testing the CRUD capabilities of the backend server, and another for testing the speed of the server. Currently, the response times for everything running on the same system (vault, backend server, client) is about 50 milliseconds. It doesn’t seem that the system gets slower with an expanding number of tokens. Currently, the number of tokens created on servers is about 14,000, which still hasn’t affected the speed.

Assuming you have already downloaded the backend server executable from the releases and configured the nksConfig.json file, you can run it via:
```
./backend <--UseSwagger> -o <your_log_file> -port <>
```
#### Known Issues:
- The C# code could produce errors if the certificate isn't trusted by your local CA.

  to fix this issue add the certificate to your local trusted certificates, under linux its in ``/usr/local/share/ca-certificates/``. 

  


<p align="right">(<a href="#readme-top">back to top</a>)</p>

---
## ‍🛡️️ Security Configuration

There is no single correct configuration for our NKS solution due to the flexibility of the tools we provide. However, here are some general guidelines and best practices to follow:

1. All Vault instances should be behind a proxy and only accessible from the other Vault instances and the C# backend server.
2. It's recommended to use Docker or Kubernetes to manage the Vault instances.
3. TODO: Create a Docker image for the backend C# server.

**Please note that the scripts we provide are merely examples of what your configuration might look like and are NOT intended for use in production environments.**
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

