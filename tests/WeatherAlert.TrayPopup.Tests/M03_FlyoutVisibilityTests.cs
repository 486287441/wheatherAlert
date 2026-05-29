using WeatherAlert.TrayPopup.Core.Flyout;
using WeatherAlert.TrayPopup.Tests.Helpers;

namespace WeatherAlert.TrayPopup.Tests;

public sealed class M03_FlyoutVisibilityTests
{
    [Fact]
    public void Initial_state_is_hidden()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler);

        Assert.Equal(FlyoutVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void OnTrayClick_toggles_visible_and_hidden()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler);

        controller.OnTrayClick();
        Assert.Equal(FlyoutVisibilityState.Visible, controller.State);

        controller.OnTrayClick();
        Assert.Equal(FlyoutVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void OnPointerLeave_schedules_hide_after_configured_delay()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler, TimeSpan.FromMilliseconds(300));
        controller.OnTrayClick();

        controller.OnPointerLeave();

        Assert.True(scheduler.HasPending);
        Assert.Equal(TimeSpan.FromMilliseconds(300), scheduler.PendingDelay);
        Assert.Equal(FlyoutVisibilityState.Visible, controller.State);

        scheduler.Elapse();
        Assert.Equal(FlyoutVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void OnPointerEnter_before_hide_delay_cancels_scheduled_hide()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler, TimeSpan.FromMilliseconds(300));
        controller.OnTrayClick();
        controller.OnPointerLeave();
        controller.OnPointerEnter();

        scheduler.Elapse();
        Assert.Equal(FlyoutVisibilityState.Visible, controller.State);
    }

    [Fact]
    public void OnPointerLeave_does_nothing_when_already_hidden()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler);

        controller.OnPointerLeave();

        Assert.False(scheduler.HasPending);
        Assert.Equal(FlyoutVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void StateChanged_fires_on_transitions()
    {
        var scheduler = new FakeHideScheduler();
        var controller = new FlyoutVisibilityController(scheduler);
        var states = new List<FlyoutVisibilityState>();

        controller.StateChanged += states.Add;
        controller.OnTrayClick();
        controller.OnTrayClick();

        Assert.Equal(
            new[] { FlyoutVisibilityState.Visible, FlyoutVisibilityState.Hidden },
            states);
    }
}
