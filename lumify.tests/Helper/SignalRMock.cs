/* SignalRMock
 * Builds a fake SignalR hub for tests.
 * Nothing is really broadcast - a "spy" client proxy records every send so a test can verify it.
 *
 * Note: Moq's Setup(...) and Verify(...) require a small lambda expression - that is simply how
 * the library is called and cannot be avoided. Everything outside of those calls is written out.
 */

using Microsoft.AspNetCore.SignalR;
using Moq;

namespace lumify.tests.Helper
{
    public static class SignalRMock
    {

        // -------------- //
        // --- Create --- //
        // -------------- //

        // Builds a fake IHubContext for the given hub type.
        // Returns the hub (inject it into the controller) and the spy (verify broadcasts on it).
        //
        // Background: Clients.Group("x").SendAsync("Event", payload) is an extension method that
        // internally calls IClientProxy.SendCoreAsync - so SendCoreAsync is what we set up and verify.
        public static (IHubContext<THub> Hub, Mock<IClientProxy> Spy) Create<THub>() where THub : Hub
        {
            // The spy proxy accepts every send and records the call for later verification.
            Mock<IClientProxy> spy = new Mock<IClientProxy>();
            spy
                .Setup(proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Every Clients.Group(...) and Clients.All call returns that same spy proxy.
            Mock<IHubClients> clients = new Mock<IHubClients>();
            clients
                .Setup(hubClients => hubClients.Group(It.IsAny<string>()))
                .Returns(spy.Object);
            clients
                .Setup(hubClients => hubClients.All)
                .Returns(spy.Object);

            // The hub context simply hands out the clients above.
            Mock<IHubContext<THub>> hub = new Mock<IHubContext<THub>>();
            hub
                .Setup(hubContext => hubContext.Clients)
                .Returns(clients.Object);

            return (hub.Object, spy);
        }


        // ------------------ //
        // --- Assertions --- //
        // ------------------ //

        // Asserts that the hub broadcast the given event the expected number of times.
        public static void AssertBroadcast(Mock<IClientProxy> spy, string eventName, Times times)
        {
            spy.Verify(
                proxy => proxy.SendCoreAsync(eventName, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                times);
        }

        // Asserts that the hub did not broadcast anything at all.
        public static void AssertSilent(Mock<IClientProxy> spy)
        {
            spy.Verify(
                proxy => proxy.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
