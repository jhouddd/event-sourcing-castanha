namespace Castanha.UseCaseTests
{
    using Xunit;
    using Castanha.Domain.Customers;
    using NSubstitute;
    using Castanha.Application;
    using Castanha.Infrastructure.Mappings;
    using System;
    using Castanha.Domain.ValueObjects;
    using Castanha.Domain.Customers.Accounts;
    using Castanha.Application.ServiceBus;

    public class AccountTests
    {
        public ICustomerReadOnlyRepository customerReadOnlyRepository;
        public ICustomerWriteOnlyRepository customerWriteOnlyRepository;
        public IPublisher bus;

        public IResponseConverter converter;

        public AccountTests()
        {
            customerReadOnlyRepository = Substitute.For<ICustomerReadOnlyRepository>();
            customerWriteOnlyRepository = Substitute.For<ICustomerWriteOnlyRepository>();  
            converter = new ResponseConverter();
            bus = Substitute.For<IPublisher>();
        }

        [Theory]
        [InlineData("08724050601", "Ivan Paulovich", 300)]
        [InlineData("08724050601", "Ivan Paulovich Pinheiro Gomes", 100)]
        [InlineData("444", "Ivan Paulovich", 500)]
        [InlineData("08724050", "Ivan Paulovich", 300)]
        public async void Register_Valid_User_Account(string personnummer, string name, double amount)
        {
            var output = Substitute.For<CustomPresenter<Application.UseCases.Register.RegisterResponse>>();

            var registerUseCase = new Application.UseCases.Register.RegisterInteractor(
                bus,
                output,
                converter
            );

            var request = new Application.UseCases.Register.RegisterCommand(
                personnummer,
                name,
                amount
            );

            await registerUseCase.Handle(request);

            Assert.Equal(request.PIN, output.Response.Customer.Personnummer);
            Assert.Equal(request.Name, output.Response.Customer.Name);
            Assert.True(Guid.Empty != output.Response.Customer.CustomerId);
            Assert.True(Guid.Empty != output.Response.Account.AccountId);
        }


        [Theory]
        [InlineData("c725315a-1de6-4bf7-aecf-3af8f0083681", 100)]
        public async void Deposit_Valid_Amount(string accountId, double amount)
        {
            var account = Substitute.For<Account>();
            var customer = Substitute.For<Customer>();
            customer.FindAccount(Arg.Any<Guid>())
                .Returns(account);

            customerReadOnlyRepository
                .GetByAccount(Guid.Parse(accountId))
                .Returns(customer);

            var output = Substitute.For<CustomPresenter<Application.UseCases.Deposit.DepositResponse>>();

            var depositUseCase = new Application.UseCases.Deposit.DepositInteractor(
                customerReadOnlyRepository,
                bus,
                output,
                converter
            );

            var request = new Application.UseCases.Deposit.DepositCommand(
                Guid.Parse(accountId),
                amount
            );

            await depositUseCase.Handle(request);

            Assert.Equal(request.Amount, output.Response.Transaction.Amount);
        }

        private int IList<T>()
        {
            throw new NotImplementedException();
        }

        [Theory]
        [InlineData("c725315a-1de6-4bf7-aecf-3af8f0083681", 100)]
        public async void Withdraw_Valid_Amount(string accountId, double amount)
        {
            Account account = Substitute.For<Account>();
            account.Deposit(new Credit(new Amount(amount)));

            var customer = Substitute.For<Customer>();
            customer.FindAccount(Arg.Any<Guid>())
                .Returns(account);

            customerReadOnlyRepository
                .GetByAccount(Guid.Parse(accountId))
                .Returns(customer);

            var output = Substitute.For<CustomPresenter<Application.UseCases.Withdraw.WithdrawResponse>>();

            var depositUseCase = new Application.UseCases.Withdraw.WithdrawInteractor(
                customerReadOnlyRepository,
                bus,
                output,
                converter
            );

            var request = new Application.UseCases.Withdraw.WithdrawCommand(
                Guid.Parse(accountId),
                amount
            );

            await depositUseCase.Handle(request);

            Assert.Equal(request.Amount, output.Response.Transaction.Amount);
        }

        [Theory]
        [InlineData(100)]
        public void Account_With_Credits_Should_Not_Allow_Close(double amount)
        {
            var account = new Account();
            account.Deposit(new Credit(new Amount(amount)));

            Assert.Throws<AccountCannotBeClosedException>(
                () => account.Close());
        }
    }
}