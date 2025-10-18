using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using EvidenceSupportTool.ViewModels;
using EvidenceSupportTool.Services;
using System.ComponentModel;

namespace EvidenceSupportTool.Tests.ViewModels
{
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<IMonitoringService> _mockMonitoringService = null!;
        private Mock<IConfigService> _mockConfigService = null!;
        private Mock<IUserInteractionService> _mockUserInteractionService = null!;
        private MainViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockMonitoringService = new Mock<IMonitoringService>();
            _mockConfigService = new Mock<IConfigService>();
            _mockUserInteractionService = new Mock<IUserInteractionService>();
            _viewModel = new MainViewModel(_mockMonitoringService.Object, _mockConfigService.Object, _mockUserInteractionService.Object);
        }

        [TestMethod]
        public void StatusText_ShouldBeInitializedCorrectly()
        {
            // テストの観点: StatusTextが初期状態で「待機中」であることを確認する。
            Assert.AreEqual("待機中", _viewModel.StatusText);
        }

        [TestMethod]
        public void StartMonitoringCommand_CanExecute_ShouldBeTrueInitially()
        {
            // テストの観点: 初期状態でStartMonitoringCommandが実行可能であることを確認する。
            Assert.IsTrue(_viewModel.StartMonitoringCommand.CanExecute(null));
        }

        [TestMethod]
        public void StopMonitoringCommand_CanExecute_ShouldBeFalseInitially()
        {
            // テストの観点: 初期状態でStopMonitoringCommandが実行不可能であることを確認する。
            Assert.IsFalse(_viewModel.StopMonitoringCommand.CanExecute(null));
        }

        [TestMethod]
        public void StartMonitoringCommand_ShouldCallMonitoringServiceStart()
        {
            // テストの観点: StartMonitoringCommand実行時にIMonitoringService.Start()が呼び出されることを確認する。
            _viewModel.StartMonitoringCommand.Execute(null);
            _mockMonitoringService.Verify(m => m.Start(), Times.Once);
        }

        [TestMethod]
        public void StartMonitoringCommand_ShouldUpdateStatusTextAndCanExecute()
        {
            // テストの観点: StartMonitoringCommand実行時にStatusTextが更新され、StartMonitoringCommandが実行不可能に、StopMonitoringCommandが実行可能になることを確認する。
            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

            _viewModel.StartMonitoringCommand.Execute(null);

            Assert.AreEqual("監視中", _viewModel.StatusText);
            Assert.IsFalse(_viewModel.StartMonitoringCommand.CanExecute(null));
            Assert.IsTrue(_viewModel.StopMonitoringCommand.CanExecute(null));
            CollectionAssert.Contains(propertyChangedEvents, nameof(MainViewModel.StatusText));
        }

        [TestMethod]
        public void StopMonitoringCommand_ShouldCallMonitoringServiceStop()
        {
            // テストの観点: StopMonitoringCommand実行時にIMonitoringService.Stop()が呼び出されることを確認する。
            _viewModel.StartMonitoringCommand.Execute(null); // 監視状態にする
            _viewModel.StopMonitoringCommand.Execute(null);
            _mockMonitoringService.Verify(m => m.Stop(), Times.Once);
        }

        [TestMethod]
        public void StopMonitoringCommand_ShouldUpdateStatusTextAndCanExecute()
        {
            // テストの観点: StopMonitoringCommand実行時にStatusTextが更新され、StartMonitoringCommandが実行可能に、StopMonitoringCommandが実行不可能になることを確認する。
            _viewModel.StartMonitoringCommand.Execute(null); // 監視状態にする

            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

            _viewModel.StopMonitoringCommand.Execute(null);

            Assert.AreEqual("待機中", _viewModel.StatusText);
            Assert.IsTrue(_viewModel.StartMonitoringCommand.CanExecute(null));
            Assert.IsFalse(_viewModel.StopMonitoringCommand.CanExecute(null));
            CollectionAssert.Contains(propertyChangedEvents, nameof(MainViewModel.StatusText));
        }
    }
}
