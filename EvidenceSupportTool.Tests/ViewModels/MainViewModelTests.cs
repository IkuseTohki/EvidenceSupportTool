using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using EvidenceSupportTool.ViewModels;
using EvidenceSupportTool.Services;
using System.ComponentModel;
using System.Collections.Generic;

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

        /// <summary>
        /// テスト観点: DropSettingFileCommandは待機中のみ実行可能であり、監視中は実行不可能であること
        /// </summary>
        [TestMethod]
        public void DropSettingFileCommand_CanExecute_ShouldBeTrueOnlyWhenWaiting()
        {
            // 初期状態（待機中）
            Assert.IsTrue(_viewModel.DropSettingFileCommand.CanExecute("test.ini"));

            // 監視開始後（監視中）
            _viewModel.StartMonitoringCommand.Execute(null);
            Assert.IsFalse(_viewModel.DropSettingFileCommand.CanExecute("test.ini"));

            // 監視停止後（待機中）
            _viewModel.StopMonitoringCommand.Execute(null);
            Assert.IsTrue(_viewModel.DropSettingFileCommand.CanExecute("test.ini"));
        }

        /// <summary>
        /// テスト観点: 有効なiniファイルがドロップされた場合、LoadSettingsが呼び出され、StatusTextが更新されること
        /// </summary>
        [TestMethod]
        public void DropSettingFileCommand_WhenValidIniFile_ShouldLoadSettingsAndUpdateStatus()
        {
            // Arrange
            var filePath = @"C:\settings\custom_setting.ini";
            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) => propertyChangedEvents.Add(e.PropertyName!);

            // Act
            _viewModel.DropSettingFileCommand.Execute(filePath);

            // Assert
            _mockConfigService.Verify(c => c.LoadSettings(filePath), Times.Once);
            Assert.AreEqual("設定ファイルを読み込みました: custom_setting.ini", _viewModel.StatusText);
            CollectionAssert.Contains(propertyChangedEvents, nameof(MainViewModel.StatusText));
        }

        /// <summary>
        /// テスト観点: ドロップパラメータとしてstring[]が渡された場合でも、先頭の有効なiniファイルが処理されること
        /// </summary>
        [TestMethod]
        public void DropSettingFileCommand_WhenFileArrayPassed_ShouldProcessFirstFile()
        {
            // Arrange
            var files = new[] { @"C:\settings\first_setting.ini", @"C:\settings\second_setting.ini" };

            // Act
            _viewModel.DropSettingFileCommand.Execute(files);

            // Assert
            _mockConfigService.Verify(c => c.LoadSettings(files[0]), Times.Once);
            Assert.AreEqual("設定ファイルを読み込みました: first_setting.ini", _viewModel.StatusText);
        }

        /// <summary>
        /// テスト観点: ドロップされたファイルの拡張子が.iniでない場合、エラーが表示されLoadSettingsは呼ばれずステータスも維持されること
        /// </summary>
        [TestMethod]
        public void DropSettingFileCommand_WhenNonIniFileDropped_ShouldShowErrorAndNotLoad()
        {
            // Arrange
            var filePath = @"C:\settings\manual.txt";

            // Act
            _viewModel.DropSettingFileCommand.Execute(filePath);

            // Assert
            _mockConfigService.Verify(c => c.LoadSettings(It.IsAny<string>()), Times.Never);
            _mockUserInteractionService.Verify(u => u.ShowError("設定ファイルには .ini ファイルを指定してください。"), Times.Once);
            Assert.AreEqual("待機中", _viewModel.StatusText);
        }

        /// <summary>
        /// テスト観点: LoadSettings実行時に例外が発生した場合、エラーが表示されステータスが維持されること
        /// </summary>
        [TestMethod]
        public void DropSettingFileCommand_WhenLoadSettingsThrows_ShouldShowErrorAndKeepStatus()
        {
            // Arrange
            var filePath = @"C:\settings\broken_setting.ini";
            _mockConfigService.Setup(c => c.LoadSettings(filePath)).Throws(new InvalidOperationException("不正な設定内容"));

            // Act
            _viewModel.DropSettingFileCommand.Execute(filePath);

            // Assert
            _mockConfigService.Verify(c => c.LoadSettings(filePath), Times.Once);
            _mockUserInteractionService.Verify(u => u.ShowError("設定ファイルの読み込みに失敗しました: 不正な設定内容"), Times.Once);
            Assert.AreEqual("待機中", _viewModel.StatusText);
        }
    }
}
