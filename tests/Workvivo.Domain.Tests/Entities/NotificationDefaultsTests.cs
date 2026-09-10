using Shouldly;
using Workvivo.Domain.Abstractions.Enums;
using Workvivo.Domain.Entities.RealTime;
using Xunit;

namespace Workvivo.Domain.Tests.Entities;

/// <summary>
/// Almost nobody changes their notification settings, so the defaults are what almost
/// everybody actually gets. Getting them wrong is how a product emails a hundred
/// thousand people every time somebody likes a post, and then gets filed as spam
/// company-wide.
/// </summary>
public class NotificationDefaultsTests
{
    [Theory]
    [InlineData(NotificationType.Mention)]
    [InlineData(NotificationType.Announcement)]
    [InlineData(NotificationType.Recognition)]
    [InlineData(NotificationType.EventInvitation)]
    public void Things_a_person_would_be_sorry_to_miss_are_emailed(NotificationType type)
    {
        NotificationDefaults.EmailEnabled(type).ShouldBeTrue();
    }

    [Theory]
    [InlineData(NotificationType.Reaction)]
    [InlineData(NotificationType.PostComment)]
    [InlineData(NotificationType.NewFollower)]
    [InlineData(NotificationType.Birthday)]
    public void Social_noise_is_not_emailed(NotificationType type)
    {
        NotificationDefaults.EmailEnabled(type).ShouldBeFalse();
    }

    [Fact]
    public void Everything_is_shown_in_the_app()
    {
        // The in-app list is cheap and dismissible; there is no type worth hiding from
        // it by default.
        foreach (var type in Enum.GetValues<NotificationType>())
        {
            NotificationDefaults.InAppEnabled(type).ShouldBeTrue();
        }
    }

    [Fact]
    public void Chatty_types_default_to_a_digest_rather_than_a_message_each()
    {
        // So that switching comment email on produces one message a day, not forty.
        NotificationDefaults.EmailFrequency(NotificationType.PostComment)
            .ShouldBe(NotificationDigestFrequency.Daily);

        NotificationDefaults.EmailFrequency(NotificationType.NewFollower)
            .ShouldBe(NotificationDigestFrequency.Weekly);
    }

    [Fact]
    public void A_frequency_of_never_refuses_email_even_when_email_is_enabled()
    {
        // Two fields can disagree, and this is the one combination a user can produce
        // by hand. "Never" has to win, or a person who set it still gets mail.
        var preference = new NotificationPreference
        {
            Email_Enabled = true,
            Email_Frequency = NotificationDigestFrequency.Never,
        };

        preference.Allows(NotificationChannel.Email).ShouldBeFalse();
    }

    [Fact]
    public void Push_is_off_until_something_can_send_it()
    {
        // The column exists so the schema is ready; there is no sender, and defaulting
        // it on would silently promise delivery that never happens.
        NotificationDefaults.For(Guid.NewGuid(), NotificationType.Mention)
            .Allows(NotificationChannel.Push)
            .ShouldBeFalse();
    }
}
