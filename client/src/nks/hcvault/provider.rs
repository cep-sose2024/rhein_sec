use std::sync::{Arc, Mutex};
use super::NksProvider;
use tracing::instrument;
/*
//TODO use CAL once it can compile
use crate::common::{
    crypto::{
        algorithms::{
            encryption::{AsymmetricEncryption, BlockCiphers},
            hashes::Hash,
        },
        KeyUsage,
    },
    error::SecurityModuleError,
    traits::module_provider::Provider,
};
*/

/// Implements the `Provider` trait, providing cryptographic operations utilizing a NKS.


//impl Provider for NksProvider {
impl NksProvider {
    /// Creates a new cryptographic key identified by `key_id`.
    ///
    /// This method generates a new cryptographic key within the NKS, using the specified
    /// algorithm, symmetric algorithm, hash algorithm, and key usages. The key is made persistent
    /// and associated with the provided `key_id`.
    ///
    /// # Arguments
    ///
    /// * `key_id` - A string slice that uniquely identifies the key to be created.
    /// * `key_algorithm` - The asymmetric encryption algorithm to be used for the key.
    /// * `sym_algorithm` - An optional symmetric encryption algorithm to be used with the key.
    /// * `hash` - An optional hash algorithm to be used with the key.
    /// * `key_usages` - A vector of `AppKeyUsage` values specifying the intended usages for the key.
    ///
    /// # Returns
    ///
    /// A `Result` that, on success, contains `Ok(())`, indicating that the key was created successfully.
    /// On failure, it returns a `SecurityModuleError`.

    //TODO implement create_key

    // #[instrument]
    // fn create_key(&mut self, key_id: &str) -> Result<(), SecurityModuleError> {
    // }

    /// Loads an existing cryptographic key identified by `key_id`.
    ///
    /// This method loads an existing cryptographic key from the NKS, using the specified
    /// algorithm, symmetric algorithm, hash algorithm, and key usages. The loaded key is
    /// associated with the provided `key_id`.
    ///
    /// # Arguments
    ///
    /// * `key_id` - A string slice that uniquely identifies the key to be loaded.
    /// * `key_algorithm` - The asymmetric encryption algorithm used for the key.
    /// * `sym_algorithm` - An optional symmetric encryption algorithm used with the key.
    /// * `hash` - An optional hash algorithm used with the key.
    /// * `key_usages` - A vector of `AppKeyUsage` values specifying the intended usages for the key.
    ///
    /// # Returns
    ///
    /// A `Result` that, on success, contains `Ok(())`, indicating that the key was loaded successfully.
    /// On failure, it returns a `SecurityModuleError`.

    //TODO implement load_key

    // #[instrument]
    // fn load_key(&mut self, key_id: &str) -> Result<(), SecurityModuleError> {
    // }

    /// Initializes the NKS module and returns a handle for further operations.
    ///
    /// This method initializes the NKS context and prepares it for use. It should be called
    /// before performing any other operations with the NKS.
    ///
    /// # Returns
    ///
    /// A `Result` that, on success, contains `Ok(())`, indicating that the module was initialized successfully.
    /// On failure, it returns a `SecurityModuleError`.

    //TODO implement initialize_module

    // #[instrument]
    // fn initialize_module(
    //     &mut self,
    //     key_algorithm: AsymmetricEncryption,
    //     sym_algorithm: Option<BlockCiphers>,
    //     hash: Option<Hash>,
    //     key_usages: Vec<KeyUsage>,
    // ) -> Result<(), SecurityModuleError> {
    // }
}
