using FluentValidation.TestHelper;
using WalletPulse.Validators;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Tests;

public class ValidatorTests
{
    [Fact]
    public void CreateWallet_InvalidType_HasError()
    {
        var validator = new CreateCustomerWalletRequestValidator();
        var model = new CreateCustomerWalletRequest
        {
            WalletName = "Personal", Type = "crypto", AccountNumber = "0244",
            AccountScheme = "MTN", CreatedAt = DateTime.UtcNow, Owner = "u1",
            CustomerId = Guid.NewGuid()
        };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void CreateWallet_ValidModel_HasNoErrors()
    {
        var validator = new CreateCustomerWalletRequestValidator();
        var model = new CreateCustomerWalletRequest
        {
            WalletName = "Personal", Type = "momo", AccountNumber = "0244",
            AccountScheme = "MTN", CreatedAt = DateTime.UtcNow, Owner = "u1",
            CustomerId = Guid.NewGuid()
        };
        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Movement_NonPositiveAmount_HasError()
    {
        var validator = new WalletMovementRequestValidator();
        var model = new WalletMovementRequest { Amount = 0m };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Movement_ThreeDecimals_HasError()
    {
        var validator = new WalletMovementRequestValidator();
        var model = new WalletMovementRequest { Amount = 10.123m };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Customer_InvalidEmail_HasError()
    {
        var validator = new CreateCustomerRequestValidator();
        var model = new CreateCustomerRequest
        {
            FirstName = "John", LastName = "Doe", BirthDate = DateTime.UtcNow,
            Email = "not-an-email", PhoneNumber = "+233200000001", Address = "Accra"
        };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
