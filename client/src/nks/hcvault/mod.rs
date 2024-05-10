//TODO use CAL once it can compile
// use crate::common::crypto::{
//     algorithms::{
//         encryption::{
//             AsymmetricEncryption, BlockCiphers, EccCurves, EccSchemeAlgorithm, SymmetricMode,
//         },
//         hashes::{Hash, Sha2Bits, Sha3Bits},
//         KeyBits,
//     },
//     KeyUsage,
// };
use std::sync::{Arc, Mutex};

pub mod key_handle;
pub mod provider;

/// A NKS-based cryptographic provider for managing cryptographic keys and performing
/// cryptographic operations.
///
/// This provider leverages the Network Key Storage (NKS) to interact with a network
/// module for operations like signing, encryption, and decryption. It provides a secure and
/// network-backed implementation of cryptographic operations.
#[derive(Clone, Debug)]
#[repr(C)]
pub struct NksProvider {
    //TODO implement NksProvider struct
    /*
    /// A unique identifier for the cryptographic key managed by this provider.
    key_id: String,
    pub(super) key_handle: Option<Arc<Mutex<TssKeyHandle>>>,
    pub(super) handle: Option<Arc<Mutex<Context>>>,
    pub(super) key_algorithm: Option<AsymmetricEncryption>,
    pub(super) sym_algorithm: Option<BlockCiphers>,
    pub(super) hash: Option<Hash>,
    pub(super) key_usages: Option<Vec<KeyUsage>>,

     */
}

impl NksProvider {
    /// Constructs a new `NksProvider`.
    ///
    /// # Arguments
    ///
    /// * `key_id` - A string identifier for the cryptographic key to be managed by this provider.
    pub fn new(key_id: String) -> Self {
        Self {
            //TODO implement NksProvider constructor
            /*
            key_id,
            key_handle: None,
            handle: None,
            key_algorithm: None,
            sym_algorithm: None,
            hash: None,
            key_usages: None,

             */
        }
    }
}

//TODO implement Enum conversions
/*
impl From<Hash> for HashingAlgorithm {
    fn from(val: Hash) -> Self {
        match val {
            Hash::Sha1 => HashingAlgorithm::Sha1,
            Hash::Sha2(bits) => match bits {
                Sha2Bits::Sha256 => HashingAlgorithm::Sha256,
                Sha2Bits::Sha384 => HashingAlgorithm::Sha384,
                Sha2Bits::Sha512 => HashingAlgorithm::Sha512,
                _ => {
                    unimplemented!()
                }
            },
            Hash::Sha3(bits) => match bits {
                Sha3Bits::Sha3_256 => HashingAlgorithm::Sha3_256,
                Sha3Bits::Sha3_384 => HashingAlgorithm::Sha3_384,
                Sha3Bits::Sha3_512 => HashingAlgorithm::Sha3_512,
                _ => {
                    unimplemented!()
                }
            },
            _ => {
                unimplemented!()
            }
        }
    }
}

impl From<EccSchemeAlgorithm> for SignatureScheme {
    fn from(value: EccSchemeAlgorithm) -> Self {
        match value {
            EccSchemeAlgorithm::EcDsa(_) => SignatureScheme::EcDsa {
                hash_scheme: HashScheme::new(HashingAlgorithm::Sha512),
            },
            EccSchemeAlgorithm::EcDaa(_) => Self::EcDaa {
                ecdaa_scheme: EcDaaScheme::new(HashingAlgorithm::Sha512, 0),
            },
            EccSchemeAlgorithm::Sm2(_) => Self::Sm2 {
                hash_scheme: HashScheme::new(HashingAlgorithm::Sha512),
            },
            EccSchemeAlgorithm::EcSchnorr(_) => SignatureScheme::EcSchnorr {
                hash_scheme: HashScheme::new(HashingAlgorithm::Sha512),
            },
            _ => unimplemented!(),
        }
    }
}

impl From<EccSchemeAlgorithm> for EccScheme {
    fn from(value: EccSchemeAlgorithm) -> Self {
        match value {
            EccSchemeAlgorithm::EcDsa(_) => {
                EccScheme::EcDsa(HashScheme::new(HashingAlgorithm::Sha512))
            }
            EccSchemeAlgorithm::EcDh(_) => {
                EccScheme::EcDh(HashScheme::new(HashingAlgorithm::Sha512))
            }
            EccSchemeAlgorithm::EcDaa(_) => {
                EccScheme::EcDaa(EcDaaScheme::new(HashingAlgorithm::Sha512, 0))
            }
            EccSchemeAlgorithm::Sm2(_) => EccScheme::Sm2(HashScheme::new(HashingAlgorithm::Sha512)),
            EccSchemeAlgorithm::EcSchnorr(_) => {
                EccScheme::EcSchnorr(HashScheme::new(HashingAlgorithm::Sha512))
            }
            EccSchemeAlgorithm::EcMqv(_) => {
                EccScheme::EcMqv(HashScheme::new(HashingAlgorithm::Sha512))
            }
            EccSchemeAlgorithm::Null => unimplemented!(),
        }
    }
}

impl From<EccCurves> for EccCurve {
    fn from(val: EccCurves) -> Self {
        match val {
            EccCurves::P256 => EccCurve::NistP256,
            EccCurves::P384 => EccCurve::NistP384,
            EccCurves::P521 => EccCurve::NistP521,
            EccCurves::Secp256k1 => EccCurve::Sm2P256,
            EccCurves::BrainpoolP256r1 => EccCurve::BnP256,
            EccCurves::BrainpoolP638 => EccCurve::BnP638,
            _ => {
                unimplemented!()
            }
        }
    }
}

impl From<KeyBits> for RsaKeyBits {
    fn from(val: KeyBits) -> Self {
        match val {
            KeyBits::Bits1024 => RsaKeyBits::Rsa1024,
            KeyBits::Bits2048 => RsaKeyBits::Rsa2048,
            KeyBits::Bits3072 => RsaKeyBits::Rsa3072,
            KeyBits::Bits4096 => RsaKeyBits::Rsa4096,
            _ => {
                unimplemented!()
            }
        }
    }
}

impl From<AsymmetricEncryption> for PublicAlgorithm {
    fn from(val: AsymmetricEncryption) -> Self {
        match val {
            AsymmetricEncryption::Rsa(_) => todo!(),
            AsymmetricEncryption::Ecc(_) => todo!(),
        }
    }
}

impl From<KeyBits> for AesKeyBits {
    fn from(val: KeyBits) -> Self {
        match val {
            KeyBits::Bits128 => AesKeyBits::Aes128,
            KeyBits::Bits192 => AesKeyBits::Aes192,
            KeyBits::Bits256 => AesKeyBits::Aes256,
            _ => unimplemented!(),
        }
    }
}

impl From<KeyBits> for CamelliaKeyBits {
    fn from(val: KeyBits) -> Self {
        match val {
            KeyBits::Bits128 => CamelliaKeyBits::Camellia128,
            KeyBits::Bits192 => CamelliaKeyBits::Camellia192,
            KeyBits::Bits256 => CamelliaKeyBits::Camellia256,
            _ => unimplemented!(),
        }
    }
}

impl From<SymmetricMode> for TssSymmetricMode {
    fn from(val: SymmetricMode) -> Self {
        match val {
            SymmetricMode::Ecb => TssSymmetricMode::Ecb,
            SymmetricMode::Cbc => TssSymmetricMode::Cbc,
            SymmetricMode::Cfb => TssSymmetricMode::Cfb,
            SymmetricMode::Ofb => TssSymmetricMode::Ofb,
            SymmetricMode::Ctr => TssSymmetricMode::Ctr,
            _ => unimplemented!(),
        }
    }
}

impl From<BlockCiphers> for SymmetricDefinitionObject {
    fn from(val: BlockCiphers) -> Self {
        match val {
            BlockCiphers::Aes(sym_mode, key_bits) => SymmetricDefinitionObject::Aes {
                key_bits: key_bits.into(),
                mode: sym_mode.into(),
            },
            BlockCiphers::Camellia(sym_mode, key_bits) => SymmetricDefinitionObject::Camellia {
                key_bits: key_bits.into(),
                mode: sym_mode.into(),
            },
            _ => unimplemented!(),
        }
    }
}

 */
