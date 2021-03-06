﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WampSharp.Cra
{
    /// <summary>
    /// WAMP-CRA Authentication Helper methods.
    /// </summary>
    public static class WampCraHelpers
    {
        /// <summary>
        /// Computes a derived cryptographic key from a password according to PBKDF2
        /// http://en.wikipedia.org/wiki/PBKDF2. The function will only return a derived key
        /// if at least 'salt' is present in the 'extra' dictionary. The complete set of
        /// attributes that can be set in 'extra': 
        ///    salt: The salt value to be used.
        ///    iterations: Number of iterations of derivation algorithm to run. 
        ///    keylen: Key length to derive.
        /// </summary>
        /// <param name="secret">The secret key from which to derive. </param>
        /// <param name="extra"> Extra data for salting the secret. Possible key values 'salt'
        /// (required, otherwise returns @secret), 'iterations' (1000 default),
        /// and/or 'keylen' (32 default). </param>
        /// <returns>A derived key (Base64 encoded) if a salt is provided in the extra parameter, or the
        /// value of parameter 'secret' if not.</returns>
        public static string DeriveKey(string secret, IDictionary<string, string> extra)
        {
            const int DEFAULT_ITERATIONS = 1000;
            const int DEFAULT_KEY_LEN = 32;

            string salt;
            if (extra == null || !extra.TryGetValue("salt", out salt))
                return secret;

            string strTemp;
            int iterations = DEFAULT_ITERATIONS;
            if (extra.TryGetValue("iterations", out strTemp))
            {
                iterations = int.TryParse(strTemp, out iterations) ? iterations : DEFAULT_ITERATIONS;
            }

            int keyLen = DEFAULT_KEY_LEN;
            if (extra.TryGetValue("keylen", out strTemp))
            {
                keyLen = int.TryParse(strTemp, out keyLen) ? keyLen : DEFAULT_KEY_LEN;
            }

            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);
            byte[] keyBytes = PBKDF2Sha256GetBytes(keyLen, secretBytes, saltBytes, iterations);
            string result = Convert.ToBase64String(keyBytes);
            Array.Clear(secretBytes, 0, secretBytes.Length);
            Array.Clear(saltBytes, 0, saltBytes.Length);
            return result;
        }

        /// <summary>
        /// Compute the authentication signature from an authentication challenge and a secret.
        /// </summary>
        /// <param name="authChallenge">The authentication challenge. </param>
        /// <param name="authSecret">The authentication secret. </param>
        /// <param name="authExtra">Extra data for salting the secret. Possible key values 'salt'
        /// (required, otherwise uses @authSecret), 'iterations' (1000
        /// default), and/or 'keylen' (32 default). </param>
        /// <returns>The authentication signature.</returns>
        public static string AuthSignature(string authChallenge, string authSecret, IDictionary<string, string> authExtra)
        {
            if (authSecret == null)
            {
                authSecret = string.Empty;
            }

            authSecret = DeriveKey(authSecret, authExtra);
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(authSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(authChallenge));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Implements PBKDF2 functionality by using a pseudorandom number generator based on
        /// HMACSHA256.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when one or more arguments are outside
        /// the required range.
        ///  </exception>
        /// <param name="dklen">The number of pseudo-random key bytes to generate. </param>
        /// <param name="password">The password used to derive the key. After use, the password
        /// and salt should be cleared (with Array.Clear) </param>
        /// <param name="salt">The key salt used to derive the key. After use, the password
        /// and salt should be cleared (with Array.Clear) </param>
        /// <param name="iterationCount">The number of iterations for the operation. The iteration
        /// count should be as high as possible without causing
        /// unreasonable delay. </param>
        /// <returns>A byte array filled with pseudo-random key bytes.</returns>
        private static byte[] PBKDF2Sha256GetBytes(int dklen, byte[] password, byte[] salt, int iterationCount)
        {
            using (HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(password))
            {
                int hashLength = hmac.HashSize/8;
                if ((hmac.HashSize & 7) != 0)
                {
                    hashLength++;
                }
                int keyLength = dklen/hashLength;
                if ((long) dklen > (0xFFFFFFFFL*hashLength) || dklen < 0)
                {
                    throw new ArgumentOutOfRangeException("dklen");
                }
                if (dklen%hashLength != 0)
                {
                    keyLength++;
                }
                byte[] extendedkey = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, extendedkey, 0, salt.Length);
                using (MemoryStream ms = new System.IO.MemoryStream())
                {
                    for (int i = 0; i < keyLength; i++)
                    {
                        extendedkey[salt.Length] = (byte) (((i + 1) >> 24) & 0xFF);
                        extendedkey[salt.Length + 1] = (byte) (((i + 1) >> 16) & 0xFF);
                        extendedkey[salt.Length + 2] = (byte) (((i + 1) >> 8) & 0xFF);
                        extendedkey[salt.Length + 3] = (byte) (((i + 1)) & 0xFF);
                        byte[] u = hmac.ComputeHash(extendedkey);
                        Array.Clear(extendedkey, salt.Length, 4);
                        byte[] f = u;
                        for (int j = 1; j < iterationCount; j++)
                        {
                            u = hmac.ComputeHash(u);
                            for (int k = 0; k < f.Length; k++)
                            {
                                f[k] ^= u[k];
                            }
                        }
                        ms.Write(f, 0, f.Length);
                        Array.Clear(u, 0, u.Length);
                        Array.Clear(f, 0, f.Length);
                    }
                    byte[] dk = new byte[dklen];
                    ms.Position = 0;
                    ms.Read(dk, 0, dklen);
                    ms.Position = 0;
                    for (long i = 0; i < ms.Length; i++)
                    {
                        ms.WriteByte(0);
                    }
                    Array.Clear(extendedkey, 0, extendedkey.Length);
                    return dk;
                }
            }
        }
    }
}
