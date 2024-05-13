use std::fmt;

/// Represents errors that can occur when interacting with a Network Key Storage (NKS).
///
/// This enum encapsulates different types of errors that may arise during NKS operations,
/// including I/O errors, HashiCorp Vault API errors, initialization errors, and unsupported operations.
/// It is designed to provide a clear and descriptive representation of the error, facilitating
/// error handling and logging.
#[derive(Debug)]
#[repr(C)]
pub enum NksError {
    /// Error related to I/O operations, wrapping a `std::io::Error`.
    Io(std::io::Error),
    //TODO implement hcvault errors
    /*
    /// Error originating from HashiCorp Vault API calls, wrapping a `hcvault::core::Error`.
    /// This variant is only available with HaschiCorp Vault NKS.
    #[cfg(feature = "hcvault")]
    Hcv(hcvault::core::Error),
     */
    /// Error occurring during NKS initialization, containing an error message.
    InitializationError(String),
    /// Error indicating that an attempted operation is unsupported, containing a description.
    UnsupportedOperation(String),
}

//TODO implement fmt::Display for NksError
/*
impl fmt::Display for NksError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}", self.description())
    }
}

 */

impl NksError {
    /// Provides a human-readable description of the NKS error.
    ///
    /// This implementation ensures that errors can be easily logged or displayed to the user,
    /// with a clear indication of the error's nature and origin.
    //TODO implement nks error
    /*
    pub fn description(&self) -> String {
        match self {
            NksError::Io(err) => format!("IO error: {}", err),
            #[cfg(feature = "win")]
            NksError::Win(err) => format!("Windows error: {}", err),
            NksError::InitializationError(msg) => format!("Initialization error: {}", msg),
            NksError::UnsupportedOperation(msg) => format!("Unsupported operation: {}", msg),
        }
    }

     */
}

/// Enables `NksError` to be treated as a trait object for any error (`dyn std::error::Error`).
///
/// This implementation allows for compatibility with Rust's standard error handling mechanisms,
/// facilitating the propagation and inspection of errors through the `source` method.

//TODO implement std::error::Error for NksError
/*
impl std::error::Error for NksError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            NksError::Io(ref err) => Some(err),
            #[cfg(feature = "win")]
            NksError::Win(ref err) => Some(err),
            // `InitializationError` and `UnsupportedOperation` do not wrap another error,
            // so they return `None` for their source.
            _ => None,
        }
    }
}

 */
