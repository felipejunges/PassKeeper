using PassKeeper.Gtk.Services;

namespace PassKeeper.Tests.Services;

public class AesEncryptionTests
{
    [Fact]
    public void AesEncryption_TestPlaceholder()
    {
        // Arrange
        var textoOriginal = "Esse é um texto de teste, que será criptografado e depois descriptografado.";
        var password = "senhaForte".ToCharArray();
        
        // Act
        var textoCriptografado = AesEncryption.Encrypt(textoOriginal, password);
        var textoDescriptografado = AesEncryption.Decrypt(textoCriptografado, password);

        // Assert
        Assert.Equal(textoOriginal, textoDescriptografado);
    }
}