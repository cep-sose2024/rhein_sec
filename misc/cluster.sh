#!/bin/bash

function start_vaults {
    for dir in vault1 vault2 vault3; do
        cd $dir
        echo "Starting Vault server in $dir..."
        nohup vault server -config=vault/VaultConfig > vaultoutput.txt 2>&1 &
        echo $! >> vault_PIDs
        cd ..
    done
    sleep 6
    cd vault1
    ./unsealVault.sh 127.0.0.1
    ./unsealVault.sh 127.0.0.2
    ./unsealVault.sh 127.0.0.3
}

function disable_vaults {
    for dir in vault1 vault2 vault3; do
        if [ -f "$dir/vault_PIDs" ]; then
            while read pid; do
                echo "Stopping Vault server with PID: $pid..."
                kill $pid
            done < "$dir/vault_PIDs"
            > "$dir/vault_PIDs"
        fi
    done
}

function setup_vaults {
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
}

case "$1" in
start)
    start_vaults
    ;;
stop)
    disable_vaults
    ;;
"")
    setup_vaults
    ;;
*)
    echo "Error: unrecognized option '$1'"
    echo "Usage: $0 {start|disable}"
    exit 1
    ;;
esac