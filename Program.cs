// See https://aka.ms/new-console-template for more information
using cvBotConsole;
using RestSharp;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{

    private static readonly string botToken = "7646768945:AAGk8BPiNS_2x-Nz_2mDA3Kx7Taww1ZI_Ns";
    private static readonly string PromptWorkExpirience = "Я пишу резюме для вакансии IT специалиста. Я написал свой опыт работы и основные достижения. Сделай этот текст лаконичным, профессиональным, выдели какие-то важные моменты более детально или перефразируй. В ответе напиши только испрваленный текст, больше ничего лишнего не пиши. В ответе не используй дополнительные символы типа жирных точек, и других эмодзи, только обычные знаки препинания. Вот текст с моим опытом работы: ";
    private static readonly string PromptAboutMe = "Я пишу резюме для вакансии IT специалиста. Я написал текст для блока \"О себе\". Сделай этот текст лаконичным, профессиональным, выдели какие-то важные моменты более детально или перефразируй. В ответе напиши только испрваленный текст, больше ничего лишнего не пиши. В ответе не используй дополнительные символы типа жирных точек, и других эмодзи, только обычные знаки препинания. Вот текст для блока \"О себе\": ";
    private static readonly TelegramBotClient botClient = new(botToken);
    private static readonly ConcurrentDictionary<long, ResumeData> userStates = new();

    static async Task Main()
    {
        Console.WriteLine("Бот запущен...");
        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
        await Task.Delay(-1); // Запускаем бесконечный цикл
    }

    private static async Task SendStartKeyboard(ITelegramBotClient bot, long chatId)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Начать заново", "reset")
        }
    });

        await bot.SendMessage(chatId, "Если хотите начать заново, нажмите на кнопку ниже.", replyMarkup: inlineKeyboard);
    }
    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken = default)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text is { } messageText)
        {
            var chatId = update.Message.Chat.Id;

            if (string.IsNullOrEmpty(messageText))
                return;

            if (messageText.Equals("/reset", StringComparison.OrdinalIgnoreCase))
            {
                userStates.TryRemove(chatId, out _);
                await bot.SendMessage(chatId, "Все данные сброшены. Введите /start");
                return;
            }


            if (!userStates.TryGetValue(chatId, out var userData))
            {
                userData = new ResumeData { Step = 0 };
                userStates[chatId] = userData;
                await bot.SendMessage(chatId, "Я помогу вам структурировать резюме!\n Введите имя:");

                return;
            }

            switch (userData.Step)
            {
                case 0: // Имя
                    if (messageText.Length < 2 || messageText.Length > 20)
                    {
                        await bot.SendMessage(chatId, "Имя должно содержать от 2 до 20 символов. Попробуйте снова:");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }
                    userData.Name = messageText;
                    userData.Step++;
                    await bot.SendMessage(chatId, "Введите контактный телефон (только цифры, 10-15 символов):");
                    await SendStartKeyboard(bot, chatId);

                    break;

                case 1: // Телефон
                    if (!Regex.IsMatch(messageText, @"^\d{10,15}$"))
                    {
                        await bot.SendMessage(chatId, "Номер телефона должен содержать только цифры и быть длиной от 10 до 15 символов. Попробуйте снова:");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }
                    userData.PhoneNumber = messageText;
                    userData.Step++;
                    await bot.SendMessage(chatId, "Введите название последней организации:");
                    await SendStartKeyboard(bot, chatId);
                    break;

                case 2: // Компания
                    if (messageText.Length < 2 || messageText.Length > 50)
                    {
                        await bot.SendMessage(chatId, "Название компании должно быть от 2 до 50 символов. Попробуйте снова:");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }
                    userData.Company = messageText;
                    userData.Step++;
                    await bot.SendMessage(chatId, "Введите дату начала работы (гггг-мм-дд):");
                    await SendStartKeyboard(bot, chatId);
                    break;

                case 3: // Дата начала работы
                    if (!DateTime.TryParseExact(messageText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
                    {
                        await bot.SendMessage(chatId, "Введите корректную дату в формате гггг-мм-дд (например, 2020-05-10):");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }
                    userData.StartDate = messageText;
                    userData.StartDateTime = startDate; // Временная переменная для проверки порядка дат
                    userData.Step++;
                    await bot.SendMessage(chatId, "Введите дату окончания работы (гггг-мм-дд):");
                    await SendStartKeyboard(bot, chatId);
                    break;

                case 4: // Дата окончания работы
                    if (!DateTime.TryParseExact(messageText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
                    {
                        await bot.SendMessage(chatId, "Введите корректную дату в формате гггг-мм-дд (например, 2022-08-15):");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }

                    if (endDate < userData.StartDateTime)
                    {
                        await bot.SendMessage(chatId, "Дата окончания работы не может быть раньше даты начала. Введите дату окончания еще раз:");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }

                    userData.EndDate = messageText;
                    userData.Step++;
                    await bot.SendMessage(chatId, "Введите занимаемую должность:");
                    await SendStartKeyboard(bot, chatId);
                    break;


                case 5: // Должность
                    if (messageText.Length < 2 || messageText.Length > 50)
                    {
                        await bot.SendMessage(chatId, "Должность должна содержать от 2 до 50 символов. Попробуйте снова:");
                        await SendStartKeyboard(bot, chatId);
                        return;
                    }
                    userData.Position = messageText;
                    userData.Step++;
                    await bot.SendMessage(chatId, "Опишите свой опыт и достижения:");
                    await SendStartKeyboard(bot, chatId);
                    break;

                case 7: // О себе
                    userData.About = messageText;

                    await bot.SendMessage(chatId, "Отправляю текст 'О себе' на улучшение...");

                    try
                    {
                        var improvedAboutMe = await ImproveExperienceTextAsync(PromptAboutMe, messageText); // Можно использовать тот же метод

                        userData.ImprovedAbout = improvedAboutMe;
                        userData.Step = 71; // промежуточный шаг

                        await bot.SendMessage(chatId, $"Вот улучшенный вариант:\n\n{improvedAboutMe}");

                        await bot.SendMessage(
                                    chatId: chatId,
                                    text: "Выберите, какой вариант сохранить:",
                                    replyMarkup: new InlineKeyboardMarkup(new[]
                                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Оставить улучшенный", "accept_improved_about"),
                            InlineKeyboardButton.WithCallbackData("🔙 Оставить свой", "accept_original_about")
                        }
                                    })
                                );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка нейросети: {ex.Message}");
                        await bot.SendMessage(chatId, "Ошибка при обработке текста нейросетью. Сохраняю оригинальный вариант.");

                        userData.Step = 8;
                        await bot.SendMessage(chatId, "Спасибо! Теперь ваше резюме почти готово.");
                        try
                        {
                            var filePath = FileWorker.GeneratePdf(userData);

                            if (!File.Exists(filePath))
                            {
                                await bot.SendMessage(chatId, "Ошибка: PDF-файл не был создан.");
                                return;
                            }

                            await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                await bot.SendDocument(chatId, new Telegram.Bot.Types.InputFileStream(fileStream, "Resume.pdf"));
                            }

                            File.Delete(filePath); // Удаляем файл после отправки
                        }
                        catch (Exception exs)
                        {
                            Console.WriteLine($"Ошибка при создании/отправке PDF: {exs.Message}");
                            await bot.SendMessage(chatId, $"Произошла ошибка: {exs.Message}");
                        }
                        // здесь можешь перейти к следующему этапу, например — генерация PDF
                    }

                    break;

                case 6: // Опыт работы
                    userData.Experience = messageText;

                    userData.WorkPlaces.Add(new WorkPlace
                    {
                        CompanyName = userData.Company,
                        StartDate = userData.StartDate,
                        EndDate = userData.EndDate,
                        PositionName = userData.Position,
                        Experience = userData.Experience
                    });

                    userData.Company = null;
                    userData.StartDate = null;
                    userData.EndDate = null;
                    userData.Position = null;
                    userData.Experience = null;

                    // Если уже 5 опытов — переход к следующему шагу
                    if (userData.WorkPlaces.Count >= 5)
                    {
                        userData.Step = 7;
                        await bot.SendMessage(chatId, "Максимум 5 рабочих мест. Переходим к разделу 'О себе':");
                        await bot.SendMessage(chatId, "Расскажите о себе:");
                        return;
                    }

                    userData.Step = 60;
                    // Показываем кнопки: добавить еще или следующий шаг
                    await bot.SendMessage(
                                chatId,
                                "Хотите добавить ещё одно место работы или перейти к следующему разделу?",
                                replyMarkup: new InlineKeyboardMarkup(new[]
                                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Улучшить опыт", "improve_experience")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➕ Добавить ещё", "add_job")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➡️ Перейти к 'О себе'", "go_about")
                    }
                                })
                            );
                    break;
            }
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackQuery = update.CallbackQuery;
            var chatId = callbackQuery.Message.Chat.Id;

            if (callbackQuery.Data == "reset")
            {
                // Сброс данных пользователя
                userStates.TryRemove(chatId, out _);
                await bot.AnswerCallbackQuery(callbackQuery.Id, "Данные сброшены. Давайте начнем заново!");
                await bot.SendMessage(chatId, "Все данные сброшены. Давайте начнем заново!");
            }
            else if (callbackQuery.Data == "accept_improved_about")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    user.About = user.ImprovedAbout;

                    await bot.SendMessage(chatId, "Улучшенный текст сохранён. Формирую резюме...");

                    try
                    {
                        var filePath = FileWorker.GeneratePdf(user);

                        if (!File.Exists(filePath))
                        {
                            await bot.SendMessage(chatId, "Ошибка: PDF-файл не был создан.");
                            return;
                        }

                        await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await bot.SendDocument(chatId, new Telegram.Bot.Types.InputFileStream(fileStream, "Resume.pdf"));
                        }

                        File.Delete(filePath); // Удаляем файл после отправки
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при создании/отправке PDF: {ex.Message}");
                        await bot.SendMessage(chatId, $"Произошла ошибка: {ex.Message}");
                    }
                }
            }
            else if (callbackQuery.Data == "accept_original_about")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    user.Step = 8;
                    await bot.SendMessage(chatId, "Оригинальный текст сохранён. Спасибо!");
                    // Здесь тоже можно переходить к генерации PDF

                    try
                    {
                        var filePath = FileWorker.GeneratePdf(user);

                        if (!File.Exists(filePath))
                        {
                            await bot.SendMessage(chatId, "Ошибка: PDF-файл не был создан.");
                            return;
                        }

                        await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await bot.SendDocument(chatId, new Telegram.Bot.Types.InputFileStream(fileStream, "Resume.pdf"));
                        }

                        File.Delete(filePath); // Удаляем файл после отправки
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при создании/отправке PDF: {ex.Message}");
                        await bot.SendMessage(chatId, $"Произошла ошибка: {ex.Message}");
                    }
                }
            }
            else if (callbackQuery.Data == "add_job")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    user.Step = 2; // начинаем ввод новой работы с названия компании
                    await bot.SendMessage(chatId, "Введите название следующей организации:");
                }
            }
            else if (callbackQuery.Data == "go_about")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    user.Step = 7;
                    await bot.SendMessage(chatId, "Расскажите о себе (не более 500 символов):");
                }
            }
            else if (callbackQuery.Data == "improve_experience")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    await bot.SendMessage(chatId, "Отправляю текст на улучшение...");

                    var currentExperience = user.WorkPlaces[^1].Experience;

                    try
                    {
                        var improvedExperience = await ImproveExperienceTextAsync(PromptWorkExpirience, currentExperience);
                        user.WorkPlaces[^1].ImprovedExperience = improvedExperience;

                        await bot.SendMessage(chatId, $"Вот улучшенный вариант:\n\n{improvedExperience}");

                        await bot.SendMessage(
                                    chatId: chatId,
                                    text: "Выберите, какой вариант сохранить:",
                                    replyMarkup: new InlineKeyboardMarkup(new[]
                                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("✅ Оставить улучшенный", "accept_improved_experience")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔙 Оставить свой", "accept_original_experience")
                            }
                                    })
                                );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка нейросети: {ex.Message}");
                        await bot.SendMessage(chatId, "Ошибка при улучшении текста. Сохраняю оригинальный.");
                    }
                }
            }
            else if (callbackQuery.Data == "accept_improved_experience")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    user.WorkPlaces[^1].Experience = user.WorkPlaces[^1].ImprovedExperience;

                    await bot.SendMessage(chatId, "Улучшенный текст сохранён!");
                    await bot.SendMessage(
                                chatId,
                                "Хотите добавить ещё одно место работы или перейти к следующему разделу?",
                                replyMarkup: new InlineKeyboardMarkup(new[]
                                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➕ Добавить ещё", "add_job"),
                        InlineKeyboardButton.WithCallbackData("➡️ Перейти к 'О себе'", "go_about")
                    }
                                })
                            );
                }
            }
            else if (callbackQuery.Data == "accept_original_experience")
            {
                if (userStates.TryGetValue(chatId, out var user))
                {
                    await bot.SendMessage(chatId, "Оригинальный текст сохранён!");
                    await bot.SendMessage(
                                chatId,
                                "Хотите добавить ещё одно место работы или перейти к следующему разделу?",
                                replyMarkup: new InlineKeyboardMarkup(new[]
                                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("➕ Добавить ещё", "add_job"),
                        InlineKeyboardButton.WithCallbackData("➡️ Перейти к 'О себе'", "go_about")
                    }
                                })
                            );
                }
            }
        }
    }

    private static async Task<string> ImproveExperienceTextAsync(string promptText, string originalText)
    {
        var client = new RestClient("https://api.intelligence.io.solutions/api/v1/chat/completions");
        var request = new RestRequest();

        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", "Bearer io-v2-eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJvd25lciI6IjNkM2Y5NjVlLWNmNmItNGMyMS1hYTcyLTVkYmE0NjYzZjkyMCIsImV4cCI6NDg5ODY3ODUxMn0.bwD041qz45lGNQgcB0wqfT_YR16JkNgO-XmRsIzsFNlt-qPqORvPuSYP34zwnjSpDq39E37KXZb2Ml0-oxp8Mg");
        request.AddHeader("content-type", "application/json");

        var body = new
        {
            model = "deepseek-ai/DeepSeek-R1",
            messages = new[]
            {
            new { role = "user", content = $"Улучши это описание опыта работы, сделай его лаконичным, профессиональным и убедительным, в ответе напиши только испрваленный текст, больше ничего лишнего не пиши. Вот исходный текст:\n\n{originalText}" }
        }
        };

        request.AddJsonBody(body);

        Console.WriteLine($"Запрос отправлен в нейросеть в: {DateTime.Now}");
        var response = await client.PostAsync(request);
        Console.WriteLine($"Запрос получен из нейросети в: {DateTime.Now}");

        var json = JsonDocument.Parse(response.Content);
        var improved = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        improved = improved.Split("</think>")[1].ToString();

        return improved;
    }


    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

}
