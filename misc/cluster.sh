#!/bin/bash

# Create a new directory called vault1 and navigate into it
mkdir vault1
cd vault1
unset VAULT_ADDR
# Copy the install.sh script into the vault1 directory and run it
cp ../install.sh .
./install.sh 127.0.0.1 8200

# Navigate back to the parent directory
cd ..

# Create a new directory called vault2 and navigate into it
mkdir vault2
cd vault2
unset VAULT_ADDR
# Copy the install.sh script into the vault2 directory and run it with the specified arguments
cp ../install.sh .
./install.sh 127.0.0.2 8200 false --nobackend

export VAULT_ADDR="https://127.0.0.2:8200"

vault operator raft join https://127.0.0.1:8200

# Navigate back to the parent directory
cd ..

# Create a new directory called vault3 and navigate into it
mkdir vault3
cd vault3
unset VAULT_ADDR
# Copy the install.sh script into the vault3 directory and run it with the specified arguments
cp ../install.sh .
./install.sh 127.0.0.3 8200 false --nobackend

export VAULT_ADDR="https://127.0.0.3:8200"

vault operator raft join https://127.0.0.1:8200

# Navigate back to the vault1 directory
cd ../vault1

# Run the unsealVault.sh script with the specified arguments
./unsealVault.sh 127.0.0.2
./unsealVault.sh 127.0.0.3