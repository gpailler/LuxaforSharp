using System;
using System.Linq;
using System.Threading;
using HidSharp;
using HidSharp.Utility;
using Moq;
using Moq.Protected;
using Xunit;

namespace LuxaforSharp.Tests
{
    public class DeviceTests
    {
        private readonly Mock<HidDevice> hidDeviceMock;
        private readonly Mock<HidStream> hidStreamMock;

        public DeviceTests()
        {
            HidSharpDiagnostics.EnableTracing = true;

            this.hidDeviceMock = new Mock<HidDevice>() { CallBase = true };
            this.hidStreamMock = new Mock<HidStream>(this.hidDeviceMock.Object) { CallBase = true };

            this.hidDeviceMock
                .Protected()
                .Setup<DeviceStream>("OpenDeviceDirectly", ItExpr.IsAny<OpenConfiguration>())
                .Returns(() => hidStreamMock.Object);
        }

        [Fact]
        public void Dispose_ActuallyDisposeUnderlyingDevice()
        {
            using (new Device(this.hidDeviceMock.Object))
            {
                this.hidStreamMock.Verify(x => x.Close(), Times.Never);
            }

            this.hidStreamMock.Verify(x => x.Close());
        }

        [Fact]
        public void SetColor_ToEveryLed_WithoutFade_UsesSimpleMode()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.All, color).Wait();
            }

            this.AssertMessagesReceived("00:01:FF:C8:14:2A:00:00:00");
        }

        [Fact]
        public void SetColor_ToSingleSide_WithoutFade_UsesSimpleMode()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.AllBackSide, color).Wait();
            }

            this.AssertMessagesReceived("00:01:42:C8:14:2A:00:00:00");
        }

        [Fact]
        public void SetColor_ToSingleLed_WithoutFade_UsesSimpleMode()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.OfIndex(5), color).Wait();
            }

            this.AssertMessagesReceived("00:01:05:C8:14:2A:00:00:00");
        }

        [Fact]
        public void SetColor_ToSingleLed_WithFade_UsesFadingMode()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.OfIndex(5), color, 64).Wait();
            }

            this.AssertMessagesReceived("00:02:05:C8:14:2A:40:00:00");
        }

        [Fact]
        public void SetColorThroughPort_ToAllLeds_SimilarResult()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.All, color, 64).Wait();
                device.AllLeds.SetColor(color, 64).Wait();
            }

            this.AssertMessagesReceived("00:02:FF:C8:14:2A:40:00:00", 2);
        }

        [Fact]
        public void SetColorThroughPort_ToFrontsidePanel_SimilarResult()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.AllFrontSide, color, 64).Wait();
                device.AllFrontsideLeds.SetColor(color, 64).Wait();
            }

            this.AssertMessagesReceived("00:02:41:C8:14:2A:40:00:00", 2);
        }

        [Fact]
        public void SetColorThroughPort_ToBacksidePanel_SimilarResult()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.AllBackSide, color, 64).Wait();
                device.AllBacksideLeds.SetColor(color, 64).Wait();
            }

            this.AssertMessagesReceived("00:02:42:C8:14:2A:40:00:00", 2);
        }

        [Fact]
        public void SetColorThroughPort_ToSingleLed_SimilarResult()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xC8, 0x14, 0x2A);
                device.SetColor(LedTarget.OfIndex(4), color, 64).Wait();
                device[4].SetColor(color, 64).Wait();
            }

            this.AssertMessagesReceived("00:02:04:C8:14:2A:40:00:00", 2);
        }

        [Fact]
        public void Blink_ToFrontSide_WithoutRepeat_LastByteIsEmpty()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xD0, 0x20, 0x20);
                device.Blink(LedTarget.AllFrontSide, color, 64).Wait();
            }

            this.AssertMessagesReceived("00:03:41:D0:20:20:40:00:00");
        }

        [Fact]
        public void Blink_ToFrontSide_WithRepeat_LastByteIsNotEmpty()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xD0, 0x20, 0x20);
                device.Blink(LedTarget.AllFrontSide, color, 64, 3).Wait();
            }

            this.AssertMessagesReceived("00:03:41:D0:20:20:40:00:03");
        }

        [Fact]
        public void Wave()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                var color = new Color(0xCC, 0xCC, 0x20);
                device.Wave(WaveType.OverlappingShort, color, 5, 2).Wait();
            }

            this.AssertMessagesReceived("00:04:03:CC:CC:20:00:02:05");
        }

        [Fact]
        public void CarryOutPattern()
        {
            using (var device = new Device(this.hidDeviceMock.Object))
            {
                device.CarryOutPattern(PatternType.RainbowWave, 3).Wait();
            }

            this.AssertMessagesReceived("00:06:08:03:00:00:00:00:00");
        }

        private void AssertMessagesReceived(string message, int callCount = 1)
        {
            var expected = this.ConvertToByteArray(message);
            this.hidStreamMock.Verify(x => x.WriteAsync(expected, 0, expected.Length, CancellationToken.None), Times.Exactly(callCount));
        }

        private byte[] ConvertToByteArray(string hexadecimalString)
        {
            return hexadecimalString
                .Split(':')
                .Select(hexadecimalNumber => Convert.ToByte(hexadecimalNumber, 16))
                .ToArray();
        }
    }
}
