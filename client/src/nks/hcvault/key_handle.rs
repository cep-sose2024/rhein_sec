use super::NksProvider;
use tracing::instrument;
/*
//TODO use CAL once it can compile
use crate::common::{
    crypto::algorithms::encryption::AsymmetricEncryption, error::SecurityModuleError,
    traits::key_handle::KeyHandle,
};
 */

//impl KeyHandle for NksProvider {
impl NksProvider {
    /// Signs the given data using the cryptographic key managed by the NKS provider.
    ///
    /// # Arguments
    ///
    /// * `data` - A byte slice representing the data to be signed.
    ///
    /// # Returns
    ///
    /// A `Result` containing the signature as a `Vec<u8>` on success, or a `SecurityModuleError` on failure.

    //TODO implement sign_data
    /*
    #[instrument]
    fn sign_data(&self, data: &[u8]) -> Result<Vec<u8>, SecurityModuleError> {
    }

     */

    /// Decrypts the given encrypted data using the cryptographic key managed by the NKS provider.
    ///
    /// # Arguments
    ///
    /// * `encrypted_data` - A byte slice representing the data to be decrypted.
    ///
    /// # Returns
    ///
    /// A `Result` containing the decrypted data as a `Vec<u8>` on success, or a `SecurityModuleError` on failure.

    //TODO implement decrypt_data
    /*
    #[instrument]
    fn decrypt_data(&self, encrypted_data: &[u8]) -> Result<Vec<u8>, SecurityModuleError> {
    }

     */

    /// Encrypts the given data using the cryptographic key managed by the NKS provider.
    ///
    /// # Arguments
    ///
    /// * `data` - A byte slice representing the data to be encrypted.
    ///
    /// # Returns
    ///
    /// A `Result` containing the encrypted data as a `Vec<u8>` on success, or a `SecurityModuleError` on failure.

    //TODO implement encrypt_data
    /*
    #[instrument]
    fn encrypt_data(&self, data: &[u8]) -> Result<Vec<u8>, SecurityModuleError> {
    }

     */

    /// Verifies the signature of the given data using the cryptographic key managed by the NKS provider.
    ///
    /// # Arguments
    ///
    /// * `data` - A byte slice representing the data whose signature is to be verified.
    /// * `signature` - A byte slice representing the signature to be verified against the data.
    ///
    /// # Returns
    ///
    /// A `Result` containing a boolean indicating whether the signature is valid (`true`) or not (`false`),
    /// or a `SecurityModuleError` on failure.

    //TODO implement verify_signature
    /*
    #[instrument]
    fn verify_signature(&self, data: &[u8], signature: &[u8]) -> Result<bool, SecurityModuleError> {
    }

     */
}
