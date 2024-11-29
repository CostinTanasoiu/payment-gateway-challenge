using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PaymentGateway.Api.Helpers;

namespace PaymentGateway.Api.Tests
{
    public class PaymentCardHelperTests
    {
        [Theory(DisplayName = "Masks card number correctly")]
        [InlineData("378282246310005", "0005")]
        [InlineData("371449635398431", "8431")]
        [InlineData("378734493671000", "1000")]
        [InlineData("5610591081018250", "8250")]
        [InlineData("30569309025904", "5904")]
        [InlineData("38520000023237", "3237")]
        [InlineData("6011111111111117", "1117")]
        [InlineData("6011000990139424", "9424")]
        [InlineData("3530111333300000", "0000")]
        [InlineData("3566002020360505", "0505")]
        [InlineData("5555555555554444", "4444")]
        [InlineData("5105105105105100", "5100")]
        [InlineData("4111111111111111", "1111")]
        [InlineData("4012888888881881", "1881")]
        [InlineData("4222222222222", "2222")]
        [InlineData("76009244561", "4561")]
        [InlineData("5019717010103742", "3742")]
        [InlineData("6331101999990016", "0016")]
        [InlineData("34623", "4623")]
        [InlineData("3889", "3889")]
        [InlineData("123", "123")]
        [InlineData("12", "12")]
        [InlineData("1", "1")]
        [InlineData("", "")]
        public void MasksCardNumberCorrectly(string cardNumber, string expectedMasked)
        {
            Assert.Equal(expectedMasked, PaymentCardHelper.MaskCardNumber(cardNumber));
        }
    }
}
