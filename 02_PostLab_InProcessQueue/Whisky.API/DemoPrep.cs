using System.Text.Json;

public static class DemoPrep
{
    public static void PrepareRatings(string ratingFolder, IWhiskyRepository? whiskyRepo, ILogger logger)
    {
        if (Directory.Exists(ratingFolder))
            return;

        Directory.CreateDirectory(ratingFolder);
        // let's generate some ratings

        logger.LogInformation("Generating test ratings");

        var random = new Random();

        var ratingMessages = new List<string>() {
                    "This whisky is great!",
                    "This whisky is ok.",
                    "This whisky is not good.",
                    "This whisky is awful.",
                    "This whisky is terrible.",
                    "Tastes like motor oil.",
                    "You can clean a bumper with this stuff.",
                    "A+++ will drink again",
                    "It killed my dog.",
                    "It was a great experience.",
                    "It was a bad experience.",
                    "It was a good experience.",
                    "It was a terrible experience.",
                    "Aight.",
                    "I'm not a fan of this whisky.",
                    "I'm a fan of this whisky.",
                    "Two thumbs up",
                    "Two thumbs down",
                    "One thumbs up",
                    "One thumbs down",
                };

        foreach (var w in whiskyRepo.GetAll(-1, -1))
        {
            logger.LogInformation($"Generating ratings for {w.Name}");

            var randomRatings = random.Next(3, 10);

            for (int x = 0; x < randomRatings; x++)
            {
                var rating = new Rating
                {
                    Stars = (short)random.Next(1, 5),
                    Message = ratingMessages[random.Next(0, ratingMessages.Count - 1)],
                };

                w.Ratings.Add(rating);
            }

            var whiskyRatingJsonPath = Path.Combine(ratingFolder, $"{w.Id}.json");
            var whiskyRatingJson = JsonSerializer.Serialize(w.Ratings);
            File.WriteAllText(whiskyRatingJsonPath, whiskyRatingJson);
        }
    }

    public static void PrepareNotifications(string notificationFolder, IWhiskyRepository? whiskyRepository, ILogger logger)
    {
        if (Directory.Exists(notificationFolder))
            return;

        var notificationFile = Path.Combine(notificationFolder, "notifications.json");

        Directory.CreateDirectory(notificationFolder);
        // let's generate some notifications

        logger.LogInformation("Generating test ratings");

        var allWhisky = whiskyRepository.GetAll(-1, -1).ToList();

        var random = new Random();
        var regions = allWhisky.Select(p => p.RegionStyle).OrderBy(p => p).Distinct().ToList();

        var notifications = new List<NotificationRequest>();
        for (int x = 0; x < 100000; x++)
        {
            var r = random.Next(0, 3);

            switch (r)
            {
                case 0:
                    notifications.Add(new NotificationRequest()
                    {
                        EmailAddress = GenerateEmailAddress(),
                        NotificationType = NotificationType.NEW_WHISKY,
                        Region = "All"
                    });
                    break;
                case 1:
                    notifications.Add(new NotificationRequest()
                    {
                        EmailAddress = GenerateEmailAddress(),
                        NotificationType = NotificationType.NEW_WHISKY_IN_REGION,
                        Region = regions[random.Next(0, regions.Count() - 1)]
                    });
                    break;
                case 2:
                    notifications.Add(new NotificationRequest()
                    {
                        EmailAddress = GenerateEmailAddress(),
                        NotificationType = NotificationType.NEW_RATING,
                        WhiskyId = allWhisky[random.Next(0, allWhisky.Count() - 1)].Id.ToString(),
                        Region = "All"
                    });
                    break;
            }
        }

        File.WriteAllText(notificationFile, JsonSerializer.Serialize(notifications));
    }

    private static string GenerateEmailAddress()
    {
        var random = new Random();
        var emailAddress = $"{random.Next(0, 999999999)}@gmail.com";
        return emailAddress;
    }
}