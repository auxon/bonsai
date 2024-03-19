using System;

namespace Bonsai.Cryptography
{
public static class EncryptionExtensions {
	public static IObservable<byte[]> Encrypt<T>(this IObservable<T> source, byte[] key, byte[] IV) where T:LambdaExpression {		
		return Observable.Create<byte[]>(o =>
			source.Subscribe(t => {
				using(var aes = Aes.Create()) {
					var encryptor = aes.CreateEncryptor(key, IV);					
					using (var ms = new MemoryStream()) {
						using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)) {
							//TODO:  Serialization issues with ExpressionTrees.  I just need LambdaExpressions
							// to be serializable, but apparently this isn't as simple as it sounds.
							// https://social.msdn.microsoft.com/Forums/en-US/cf4eab1b-98d7-4049-9bee-3b43b1cf7608/why-expression-trees-are-not-serializable?forum=linqprojectgeneral
							var formatter = new System.Runtime.Serialization.Json.DataContractJsonSerializer(t.GetType(), "root");
							
							formatter.WriteObject(cs, t);					
						}
						var encrypted = ms.ToArray();
						o.OnNext(encrypted);
					}
      			}
			})
		);
	}
	
	public static IObservable<T> Decrypt<T>(this IObservable<byte[]> source, byte[] key, byte[] IV) {		
		return Observable.Create<T>(o =>
			source.Subscribe(t => {
				using(var aes = Aes.Create()) {
					var decryptor = aes.CreateDecryptor(key, IV);												
					using (var ms = new MemoryStream(t)) {
						using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
							var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
							T decrypted = (T)formatter.Deserialize(cs);
							try {
								o.OnNext(decrypted);
							} catch(CryptographicException cex) {
								o.OnError(cex);
							}
						}						
					}
      			}
			})
		);
	}
}
