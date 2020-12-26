using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using kinodrom_bot.Service;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace kinodrom_bot
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static database db;
        private static string server_url_for_statistic = "";
        public static X509Certificate2 cback_cert;

        static async Task Main()
        {

            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();


            db = new database(Configuration.GetSection("db_connectionstring").Value);
            string BotToken = Configuration.GetSection("bot_token").Value;
            server_url_for_statistic = Configuration.GetSection("statistic_url").Value;
            Console.WriteLine("Statistic server url: " + server_url_for_statistic);
            cback_cert = CertificateUtility.GetHttpsCertificateFromStore();
            if (cback_cert == null)
            {
                Console.WriteLine("FAILED TO LOAD SSL CERTIFICATE FROM STORE");
            }
            Bot = new TelegramBotClient(BotToken);
            //#if USE_PROXY
            //            var Proxy = new WebProxy(Configuration.Proxy.Host, Configuration.Proxy.Port) { UseDefaultCredentials = true };
            //            Bot = new TelegramBotClient(Configuration.BotToken, webProxy: Proxy);
            //#else
            //            Bot = new TelegramBotClient(BotToken);
            //#endif

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            Bot.StopReceiving();
        }


        static async Task BotOnException(long chat_id, string exceptionMsg)
        {
            await Bot.SendTextMessageAsync(
                    chatId: chat_id,
                    text: exceptionMsg
                );
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;

            switch (message.Text.Split(' ').First())
            {

                case "/kinodrom@DevService_bot":
                case "/sessions":
                case "/sessions@DevService_bot":
                    await SendSessionsList(message);
                    break;

                case "/newyearstats":
                case "/newyearstats@DevService_bot":
                    //await SalesList(message);
                    await NewYearDaysButtons(message);
                    break;
                //// Send inline keyboard
                //case "/inline":
                //    await SendInlineKeyboard(message);
                //    break;

                //// send custom keyboard
                //case "/keyboard":
                //    await SendReplyKeyboard(message);
                //    break;

                //// send a photo
                //case "/photo":
                //    await SendDocument(message);
                //    break;

                //// request location or contact
                //case "/request":
                //    await RequestContactAndLocation(message);
                //    break;

                default:
                    await mxUsage(message);
                    break;
            }

            async Task SendSessionsList(Message msg)
            {
                List<database.SessionInfo> sessions_dc = new List<database.SessionInfo>();
                try
                {
                    sessions_dc = db.GetSessions();
                }
                catch (Exception e)
                {
                    await BotOnException(message.Chat.Id, $"db.GetSessions exception:{e.Message}");
                    return;
                }
                if (sessions_dc.Count == 0)
                {
                    await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Сеанси не знайдено"
                    );

                    return;
                }


                var rows = new List<List<InlineKeyboardButton>>();
                foreach (var se in sessions_dc)
                {
                    List<InlineKeyboardButton> button = new List<InlineKeyboardButton>();
                    button.Add(InlineKeyboardButton.WithCallbackData($"{se.showtime.ToString("HH:mm  dd.MM ")}  {se.FilmTitle}", $"session_id-{se.sessionId}"));
                    rows.Add(button);
                }

                var inlineKeyboard = new InlineKeyboardMarkup(rows.ToArray().ToArray());

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Оберіть сеанс:",
                    replyMarkup: inlineKeyboard
                );
            }
            async Task NewYearDaysButtons(Message msg)
            {
                string[] days = { "24.12", "25.12", "26.12", "27.12", "28.12", "29.12", "29.12", "30.12", "31.12" };

                var rows = new List<List<InlineKeyboardButton>>();
                for (int i = 0; i < days.Length;)
                {
                    List<InlineKeyboardButton> button = new List<InlineKeyboardButton>();
                    button.Add(InlineKeyboardButton.WithCallbackData($"{days[i]}", $"days-{days[i]}"));
                    i += 1;
                    button.Add(InlineKeyboardButton.WithCallbackData($"{days[i]}", $"days-{days[i]}"));
                    i += 1;
                    button.Add(InlineKeyboardButton.WithCallbackData($"{days[i]}", $"days-{days[i]}"));
                    i += 1;
                    rows.Add(button);
                }

                List<InlineKeyboardButton> buttonTotal = new List<InlineKeyboardButton>();
                buttonTotal.Add(InlineKeyboardButton.WithCallbackData($"Total", $"days-total"));
                rows.Add(buttonTotal);

                var inlineKeyboard = new InlineKeyboardMarkup(rows.ToArray().ToArray());

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Оберіть дату:",
                    replyMarkup: inlineKeyboard
                );
            }


            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            async Task SendInlineKeyboard(Message msg)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                // Simulate longer running task
                await Task.Delay(500);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard
                );
            }

            async Task SendReplyKeyboard(Message msg)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { "1.1", "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                    },
                    resizeKeyboard: true
                );

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup

                );
            }

            async Task SendDocument(Message msg)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string filePath = @"Files/tux.png";
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture"
                );
            }

            async Task RequestContactAndLocation(Message msg)
            {
                var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });
                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: RequestReplyKeyboard
                );
            }

            async Task Usage(Message msg)
            {
                const string usage = "Usage:\n" +
                                        "/inline   - send inline keyboard\n" +
                                        "/keyboard - send custom keyboard\n" +
                                        "/photo    - send a photo\n" +
                                        "/request  - request location or contact";
                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }

            async Task mxUsage(Message msg)
            {
                const string usage = "Usage:\n" +
                                        "/sessions - show todays sessions list\n" +
                                        "/newyearstats - show customer sales for party";
                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        private static async Task SalesList(Message msg)
        {

            NewYearStatisticService instance = new NewYearStatisticService(server_url_for_statistic);
            var data = instance.Customers();
            if (data.Count == 0)
            {
                await Bot.SendTextMessageAsync(
                   chatId: msg.Chat.Id,
                   text: "Інформація відсутня"
               );
                return;
            }
            if (data.Count == 1 && data[0].error.Length > 0)
            {
                await Bot.SendTextMessageAsync(
                   chatId: msg.Chat.Id,
                   text: "Помилка виконання запиту:\n" + data[0].error
               );
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"<b>Гостей усього: {data.Count}</b>\n");
            foreach (var row in data)
            {
                sb.Append($"Телефон клієнта {row.phone}\nEmail клієнта {row.Email}  {row.Qty}шт");
            }
            sb.ToString();

            await Bot.SendTextMessageAsync(
                  chatId: msg.Chat.Id,
                  text: sb.ToString()
              );
        }

        // Process Inline Keyboard callback data
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            if (callbackQuery.Data.Contains("session_id-"))
            {
                List<database.singleOrderInfo> order_info = new List<database.singleOrderInfo>();
                var incoming_msg = callbackQuery.Data;
                var session_id = Convert.ToInt32(callbackQuery.Data.Substring(incoming_msg.IndexOf('-') + 1));
                try
                {
                    order_info = db.GetKinodromOrders(session_id);
                }
                catch (Exception e)
                {
                    await BotOnException(callbackQuery.Message.Chat.Id, $"db.GetKinodromOrders exception:{e.Message}");
                    return;
                }
                if (order_info.Count > 0)
                {
                    try
                    {
                        order_info = db.GetKinodromOrders_seats(order_info);
                    }
                    catch (Exception e)
                    {
                        await BotOnException(callbackQuery.Message.Chat.Id, $"db.GetKinodromOrders_seats exception:{e.Message}");
                        return;
                    }


                    var message = MessageTemplateFormat(order_info);
                    await Bot.SendTextMessageAsync(
                             chatId: callbackQuery.Message.Chat.Id,
                            text: message,
                            parseMode: ParseMode.Html
                        );
                }
                else
                {
                    await Bot.SendTextMessageAsync(
                         chatId: callbackQuery.Message.Chat.Id,
                        text: "Замовлення відсутні"
                    );
                }
                return;
            }

            if (callbackQuery.Data.Contains("days-"))
            {
                var incoming_msg = callbackQuery.Data;
                var dayValue = callbackQuery.Data.Substring(incoming_msg.IndexOf('-') + 1);
                Console.WriteLine("Statistic server url: " + server_url_for_statistic);
                List<NewYearStatisticModel> data = new List<NewYearStatisticModel>();
                try
                {
                    NewYearStatisticService instance = new NewYearStatisticService(server_url_for_statistic);
                    try
                    {
                        data = instance.Customers(cback_cert);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Ошибка выполнения GET запроса:\n"+ ex.StackTrace);
                    }
                    if (data == null)
                        throw new Exception("reporting service null result");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                      await Bot.SendTextMessageAsync(
                       chatId: callbackQuery.Message.Chat.Id,
                       text: "Ошибка: "+ex.Message
                   );
                    return;
                }
               
                if (data.Count == 0)
                {
                    await Bot.AnswerCallbackQueryAsync(
                       callbackQueryId: callbackQuery.Id,
                       text: "Інформація відсутня"
                    );
                    return;
                }
                if (data.Count == 1 && data[0].error.Length > 0)
                {
                    await Bot.AnswerCallbackQueryAsync(
                       callbackQueryId: callbackQuery.Id,
                       text: "Помилка виконання запиту:\n" + data[0].error
                   );
                    return;
                }
                //FILTER VALUE
                var filtered = data;
                StringBuilder sb = new StringBuilder();
                sb.Append($"Транзакцій загалом: {data.Count}\n");
                if (dayValue != "total")
                {
                    var dt = DateTime.Parse(dayValue + ".2020");                    
                    filtered = data.Where(x => x.Time.Date == dt.Date).ToList();
                    if (filtered.Count == 0)
                    {
                        sb.Append($"{dt.ToString("dd.MM")} продажі відсутні");
                    }
                    else
                    {
                        sb.Append($"Придбано за {dt.ToString("dd.MM")}:\n");                      
                    }
                }
                
                foreach (var row in filtered)
                {
                    sb.Append($"\nТелефон клієнта {row.phone}\nEmail клієнта {row.Email}  {row.Qty}шт\n");
                }
                string message = sb.ToString();
                
                await Bot.SendTextMessageAsync(
                   chatId: callbackQuery.Message.Chat.Id,
                   text: message
               );
                return;
            }

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}"
            );
        }



        #region Inline Mode
        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }

        private static string MessageTemplateFormat(List<database.singleOrderInfo> order_info)
        {

            //        Cеанс: 10:05
            //--------------------------------------
            //Имейл: antonio.onischenko @gmail.com
            // Номер телефона: 380638427889
            //Содержимое заказа:
            //Акція Попкорн сир 3,0л + Пепсі 0,8л(розлив)
            //Кол - во товаров: 1
            //Сумма заказа: 85грн.
            //--------------------------------------
            string divider = "--------------------------------------";
            string template = "Номер замовлення: {bnum}\nТелефон: {phone}\nEmail: {email}\n{items}{tickets}Вартість замовлення: {price}грн\n{divider}\n";
            string ResultMessage = $"Сеанс: {order_info[0].ShowTime.ToString("dd.MM HH:mm")}   {order_info[0].FilmTitle}\n{divider}\n".Replace("{divider}", divider);
            ResultMessage = ResultMessage.Replace("{session_time}", "Время сеанса");
            foreach (var x in order_info)
            {
                string dat_message = template;
                dat_message = dat_message.Replace("{divider}", divider);
                dat_message = dat_message.Replace("{bnum}", x.BookingNumber.ToString());
                dat_message = dat_message.Replace("{phone}", x.Phone.ToString());
                dat_message = dat_message.Replace("{email}", x.Email.ToString());
                dat_message = dat_message.Replace("{price}", x.total_price.ToString("N1"));
                var items = "<i>Перелік продукції:</i>\n";
                foreach (var i in x.items)
                {
                    items += $"{i.qty}шт <b>{i.name}</b>\n";
                }
                var tickets = "<i>Інформація про місця:</i>\n";
                foreach (var t in x.tickets)
                {
                    tickets += $"Ряд:{t.RowId} місце:{t.SeatId}\n";
                }
                dat_message = dat_message.Replace("{items}", items);
                dat_message = dat_message.Replace("{tickets}", tickets);
                ResultMessage += dat_message;
            }
            return ResultMessage;
        }
    }


    public static class CertificateUtility
    {
        public static X509Certificate2 GetHttpsCertificateFromStore()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var currentCerts = certCollection.Find(X509FindType.FindBySubjectName, "cback-prod.mx.local", false);

                if (currentCerts.Count == 0)
                {
                    throw new Exception("Https certificate is not found.");
                }
                foreach (var cc in currentCerts)
                {
                    Console.WriteLine(cc.FriendlyName + "  " + cc.PrivateKey);
                }
                return currentCerts[0];
            }
        }
    }
}
