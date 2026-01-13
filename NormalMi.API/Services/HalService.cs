using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using OfficeOpenXml;
using NormalMi.API.Models;

namespace NormalMi.API.Services;

public class HalService : IHalService
{
    private readonly HttpClient _httpClient;
    private readonly ProduceCacheService? _cacheService;
    private const string HalUrl = "https://www.hal.gov.tr/Sayfalar/FiyatDetaylari.aspx";
    private string? _viewState;
    private string? _viewStateGenerator;
    private string? _eventValidation;
    private string? _tableId;

    public HalService(HttpClient httpClient, ProduceCacheService cacheService)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
    }


    public async Task<List<Produce>> GetProduceListAsync()
    {
        // Sadece cache'den döndür, Excel çekme
        // Excel sadece background service tarafından saat 04:00'te çekilecek
        if (_cacheService != null)
        {
            // Thread-safe cache kontrolü
            var isCacheValid = _cacheService.IsCacheValid();
            var cached = _cacheService.GetCachedProduces();
            var lastUpdate = _cacheService.GetLastUpdateTime();
            
            Console.WriteLine($"[GetProduceListAsync] Cache kontrolü: Geçerli={isCacheValid}, Ürün sayısı={cached.Count}, Son güncelleme={lastUpdate:yyyy-MM-dd HH:mm:ss}");
            
            if (isCacheValid && cached.Any())
            {
                Console.WriteLine($"✓ Cache'den {cached.Count} ürün döndürülüyor (Son güncelleme: {lastUpdate:yyyy-MM-dd HH:mm:ss})");
                return cached;
            }
            
            // Cache geçersiz veya boşsa, boş liste döndür (Excel çekme)
            // Excel sadece background service tarafından saat 04:00'te çekilecek
            if (!isCacheValid)
            {
                Console.WriteLine($"⚠ Cache geçersiz (24 saatten eski veya hiç doldurulmamış). Boş liste döndürülüyor. Excel sadece background service tarafından saat 04:00'te çekilecek.");
            }
            else if (!cached.Any())
            {
                Console.WriteLine($"⚠ Cache boş. Boş liste döndürülüyor. Excel sadece background service tarafından saat 04:00'te çekilecek.");
            }
        }
        else
        {
            Console.WriteLine("⚠ Cache servisi bulunamadı. Boş liste döndürülüyor.");
        }

        // Cache yoksa veya geçersizse boş liste döndür (Excel çekme)
        // Excel sadece background service tarafından saat 04:00'te çekilecek
        return new List<Produce>();
    }

    public async Task<List<Produce>> RefreshProduceListAsync()
    {
        try
        {
            Console.WriteLine("[RefreshProduceListAsync] Excel'den veri çekiliyor...");
            
            // Sadece Excel'den veri çek (HTML parsing kaldırıldı)
            var excelProduces = await GetProduceListFromExcelAsync();
            if (excelProduces != null && excelProduces.Any())
            {
                Console.WriteLine($"[RefreshProduceListAsync] Excel'den {excelProduces.Count} ürün çekildi");
                
                // Cache'i güncelle
                if (_cacheService != null)
                {
                    _cacheService.UpdateCache(excelProduces);
                    
                    // Cache'in güncellendiğini doğrula
                    var cachedCount = _cacheService.GetCachedProduces().Count;
                    var lastUpdate = _cacheService.GetLastUpdateTime();
                    Console.WriteLine($"[RefreshProduceListAsync] Cache güncellendi: {cachedCount} ürün, Tarih: {lastUpdate:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine("[RefreshProduceListAsync] ⚠ Cache servisi null!");
                }
                
                return excelProduces;
            }

            // Excel başarısız olursa hata logla ve boş liste döndür
            Console.WriteLine("⚠ Excel indirme başarısız! Veri çekilemedi.");
            return new List<Produce>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HAL servisinden veri çekilirken hata: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Produce>();
        }
    }

    private async Task<List<Produce>> GetProduceListFromExcelAsync()
    {
        try
        {
            // İlk sayfayı GET ile çek
            var firstPageHtml = await _httpClient.GetStringAsync(HalUrl);
            var firstPageDoc = new HtmlDocument();
            firstPageDoc.LoadHtml(firstPageHtml);

            // ViewState ve diğer form verilerini al
            ExtractFormData(firstPageDoc);

            // Excel indirme butonunun ID'sini bul
            var excelButton = firstPageDoc.DocumentNode.SelectSingleNode("//input[@type='submit' and contains(@value, 'Excel') or contains(@id, 'Excel')]");
            if (excelButton == null)
            {
                // Alternatif: "Excele Çıkar" butonunu bul
                excelButton = firstPageDoc.DocumentNode.SelectSingleNode("//input[contains(@value, 'Excel') or contains(@value, 'Çıkar')]");
            }

            if (excelButton == null)
            {
                Console.WriteLine("Excel indirme butonu bulunamadı");
                return new List<Produce>();
            }

            var excelButtonId = excelButton.GetAttributeValue("id", "");
            var excelButtonName = excelButton.GetAttributeValue("name", "");

            // "Tüm Sayfalar" radio button'unu bul
            // Radio button'lar genellikle "Aktarma Seçenekleri" bölümünde
            var allPagesRadio = firstPageDoc.DocumentNode.SelectSingleNode("//input[@type='radio' and (contains(@value, 'Tüm') or contains(@id, 'Tüm') or contains(@id, 'All'))]");
            
            if (allPagesRadio == null)
            {
                // Alternatif: Tüm radio button'ları bul ve "Tüm Sayfalar" text'ini içereni seç
                var radioButtons = firstPageDoc.DocumentNode.SelectNodes("//input[@type='radio']");
                if (radioButtons != null)
                {
                    foreach (var radio in radioButtons)
                    {
                        // Parent veya sibling'de "Tüm Sayfalar" text'ini ara
                        var parent = radio.ParentNode;
                        var sibling = radio.NextSibling;
                        var text = (parent?.InnerText ?? "") + (sibling?.InnerText ?? "");
                        
                        if (text.Contains("Tüm Sayfalar") || text.Contains("Tüm") || 
                            radio.GetAttributeValue("value", "").Contains("All") ||
                            radio.GetAttributeValue("id", "").Contains("All"))
                        {
                            allPagesRadio = radio;
                            break;
                        }
                    }
                }
            }

            // Tüm radio button'ları logla (debug için)
            var allRadios = firstPageDoc.DocumentNode.SelectNodes("//input[@type='radio']");
            if (allRadios != null)
            {
                Console.WriteLine($"Tüm radio button'lar ({allRadios.Count} adet):");
                foreach (var radio in allRadios)
                {
                    var name = radio.GetAttributeValue("name", "");
                    var value = radio.GetAttributeValue("value", "");
                    var id = radio.GetAttributeValue("id", "");
                    var checkedAttr = radio.GetAttributeValue("checked", "");
                    var parentText = radio.ParentNode?.InnerText?.Trim() ?? "";
                    Console.WriteLine($"  - name={name}, value={value}, id={id}, checked={checkedAttr}, parentText={parentText.Substring(0, Math.Min(50, parentText.Length))}");
                }
            }

            if (allPagesRadio == null)
            {
                Console.WriteLine("'Tüm Sayfalar' radio button bulunamadı, value='2' olan radio button kullanılacak...");
                // value="2" genellikle "Tüm Sayfalar" seçeneğidir
                allPagesRadio = firstPageDoc.DocumentNode.SelectSingleNode("//input[@type='radio' and @value='2']");
            }

            if (allPagesRadio == null)
            {
                Console.WriteLine("Radio button bulunamadı, Excel indirme iptal ediliyor");
                return new List<Produce>();
            }

            var allPagesRadioName = allPagesRadio.GetAttributeValue("name", "");
            var allPagesRadioValue = allPagesRadio.GetAttributeValue("value", "");
            
            // Eğer value boşsa, id'den veya checked attribute'undan anlamaya çalış
            if (string.IsNullOrEmpty(allPagesRadioValue))
            {
                allPagesRadioValue = "2"; // Varsayılan değer (genellikle "Tüm Sayfalar")
            }

            Console.WriteLine($"Excel butonu bulundu: id={excelButtonId}, name={excelButtonName}");
            Console.WriteLine($"Radio butonu bulundu: name={allPagesRadioName}, value={allPagesRadioValue}");

            // Excel indirme POST isteği
            var postData = new Dictionary<string, string>
            {
                { "__VIEWSTATE", _viewState ?? "" },
                { "__VIEWSTATEGENERATOR", _viewStateGenerator ?? "" },
                { "__EVENTVALIDATION", _eventValidation ?? "" }
            };

            // Tüm Sayfalar radio button'unu seç (value="2" genellikle "Tüm Sayfalar"dır)
            if (!string.IsNullOrEmpty(allPagesRadioName))
            {
                postData[allPagesRadioName] = allPagesRadioValue;
            }

            // Excel butonunu tetikle
            if (!string.IsNullOrEmpty(excelButtonName))
            {
                postData[excelButtonName] = excelButton.GetAttributeValue("value", "Excel");
            }
            else if (!string.IsNullOrEmpty(excelButtonId))
            {
                // ID varsa, name olarak kullan (ASP.NET format)
                var buttonName = excelButtonId.Replace("_", "$");
                postData[buttonName] = excelButton.GetAttributeValue("value", "Excel");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, HalUrl)
            {
                Content = new FormUrlEncodedContent(postData)
            };
            request.Headers.Add("Referer", HalUrl);

            var response = await _httpClient.SendAsync(request);
            
            // Content-Type kontrolü
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var contentDisposition = response.Content.Headers.ContentDisposition?.FileName ?? "";
            Console.WriteLine($"Response Content-Type: {contentType}");
            Console.WriteLine($"Response Content-Disposition: {contentDisposition}");
            
            // İlk birkaç byte'ı kontrol et (magic number)
            var buffer = new byte[8];
            var stream = await response.Content.ReadAsStreamAsync();
            await stream.ReadAsync(buffer, 0, 8);
            stream.Position = 0; // Reset stream
            
            var fileHeader = BitConverter.ToString(buffer);
            Console.WriteLine($"Dosya başlığı (ilk 8 byte): {fileHeader}");
            
            // Excel dosyası mı kontrol et (Excel dosyaları PK (ZIP) ile başlar)
            // Eski XLS formatı HTML tablosu olarak geliyor
            var excelBytes = await response.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"Excel dosyası indirildi: {excelBytes.Length} byte");

            // Eski XLS formatı HTML tablosu olarak geliyor, HTML olarak parse et
            if (!fileHeader.StartsWith("50-4B")) // PK = ZIP signature (modern XLSX)
            {
                Console.WriteLine("Eski XLS formatı tespit edildi, HTML tablosu olarak parse ediliyor...");
                
                // UTF-16 veya UTF-8 olabilir, her ikisini de dene
                string htmlContent;
                try
                {
                    // UTF-16 (BOM: FF-FE) olarak dene
                    htmlContent = System.Text.Encoding.Unicode.GetString(excelBytes);
                }
                catch
                {
                    // UTF-8 olarak dene
                    htmlContent = System.Text.Encoding.UTF8.GetString(excelBytes);
                }

                // HTML içeriğinin ilk 500 karakterini logla (debug için)
                var preview = htmlContent.Length > 500 ? htmlContent.Substring(0, 500) : htmlContent;
                Console.WriteLine($"HTML içeriği önizleme (ilk 500 karakter): {preview}");

                // HTML tablosunu parse et
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                var produces = new List<Produce>();
                var tables = htmlDoc.DocumentNode.SelectNodes("//table");
                
                if (tables == null || tables.Count == 0)
                {
                    Console.WriteLine("HTML'de tablo bulunamadı");
                    return new List<Produce>();
                }

                Console.WriteLine($"{tables.Count} tablo bulundu, doğru tablo aranıyor...");
                
                // Tüm tabloları dolaş ve doğru tabloyu bul
                HtmlNode? dataTable = null;
                int headerRowIndex = -1;
                
                foreach (var table in tables)
                {
                    var rows = table.SelectNodes(".//tr");
                    if (rows == null || rows.Count < 2)
                    {
                        continue; // Yeterli satır yok
                    }

                    Console.WriteLine($"Tablo kontrol ediliyor: {rows.Count} satır");

                    // Her satırı kontrol et, başlık satırını bul
                    for (int rowIdx = 0; rowIdx < Math.Min(rows.Count, 5); rowIdx++) // İlk 5 satırı kontrol et
                    {
                        var cells = rows[rowIdx].SelectNodes(".//td | .//th");
                        if (cells == null || cells.Count < 4) continue;

                        var cellTexts = cells.Select(c => c.InnerText?.Trim() ?? "").ToList();
                        var cellTextsLower = cellTexts.Select(t => t.ToLowerInvariant()).ToList();
                        
                        // Başlık satırını tespit et: "Ürün Adı", "Ürün Cinsi", "Minimum Fiyat", "Maksimum Fiyat" içermeli
                        bool hasProductName = cellTextsLower.Any(t => t.Contains("ürün adı") || t.Contains("ürün"));
                        bool hasProductType = cellTextsLower.Any(t => t.Contains("ürün cinsi") || t.Contains("cins"));
                        bool hasMinPrice = cellTextsLower.Any(t => t.Contains("minimum fiyat") || t.Contains("minumum fiyat") || t.Contains("min"));
                        bool hasMaxPrice = cellTextsLower.Any(t => t.Contains("maksimum fiyat") || t.Contains("maks") || t.Contains("max"));

                        if (hasProductName && hasProductType && (hasMinPrice || hasMaxPrice))
                        {
                            Console.WriteLine($"Doğru tablo bulundu! Başlık satırı: {rowIdx + 1}. satır");
                            Console.WriteLine($"Başlık sütunları: {string.Join(" | ", cellTexts)}");
                            dataTable = table;
                            headerRowIndex = rowIdx;
                            break;
                        }
                    }

                    if (dataTable != null) break; // Doğru tablo bulundu
                }

                if (dataTable == null)
                {
                    Console.WriteLine("Ürün verisi içeren tablo bulunamadı");
                    return new List<Produce>();
                }

                // Doğru tabloyu parse et
                var dataRows = dataTable.SelectNodes(".//tr");
                if (dataRows == null || dataRows.Count <= headerRowIndex + 1)
                {
                    Console.WriteLine("Veri satırı bulunamadı");
                    return new List<Produce>();
                }

                Console.WriteLine($"Veri satırları parse ediliyor (başlık: {headerRowIndex + 1}. satır, toplam: {dataRows.Count} satır)");

                // Başlık satırından sütun indekslerini bul
                var headerCells = dataRows[headerRowIndex].SelectNodes(".//td | .//th");
                if (headerCells == null)
                {
                    Console.WriteLine("Başlık satırı hücreleri bulunamadı");
                    return new List<Produce>();
                }

                // Başlık satırını detaylı logla
                Console.WriteLine($"Başlık satırı ({headerCells.Count} sütun):");
                for (int colIdx = 0; colIdx < headerCells.Count; colIdx++)
                {
                    var headerText = headerCells[colIdx].InnerText?.Trim() ?? "";
                    Console.WriteLine($"  Sütun {colIdx}: '{headerText}'");
                }

                int colProductName = -1, colProductType = -1, colMinPrice = -1, colMaxPrice = -1, colAveragePrice = -1, colVolume = -1, colUnit = -1;
                
                for (int colIdx = 0; colIdx < headerCells.Count; colIdx++)
                {
                    var headerText = headerCells[colIdx].InnerText?.Trim() ?? "";
                    var headerTextLower = headerText.ToLowerInvariant();
                    
                    if ((headerTextLower.Contains("ürün adı") || (headerTextLower.Contains("ürün") && !headerTextLower.Contains("cins") && !headerTextLower.Contains("türü"))) && colProductName == -1)
                        colProductName = colIdx;
                    else if ((headerTextLower.Contains("ürün cinsi") || headerTextLower.Contains("cins")) && colProductType == -1)
                        colProductType = colIdx;
                    else if ((headerTextLower.Contains("minimum fiyat") || headerTextLower.Contains("minumum fiyat") || (headerTextLower.Contains("min") && headerTextLower.Contains("fiyat"))) && colMinPrice == -1)
                        colMinPrice = colIdx;
                    else if ((headerTextLower.Contains("maksimum fiyat") || (headerTextLower.Contains("maks") && headerTextLower.Contains("fiyat"))) && colMaxPrice == -1)
                        colMaxPrice = colIdx;
                    else if ((headerTextLower.Contains("ortalama fiyat") || (headerTextLower.Contains("ortalama") && headerTextLower.Contains("fiyat"))) && colAveragePrice == -1)
                        colAveragePrice = colIdx;
                    else if ((headerTextLower.Contains("işlem hacmi") || headerTextLower.Contains("hacim")) && colVolume == -1)
                        colVolume = colIdx;
                    else if (headerTextLower.Contains("birim") && colUnit == -1)
                        colUnit = colIdx;
                }

                Console.WriteLine($"Sütun indeksleri: Ürün Adı={colProductName}, Ürün Cinsi={colProductType}, Min Fiyat={colMinPrice}, Max Fiyat={colMaxPrice}, Ortalama Fiyat={colAveragePrice}, Hacim={colVolume}, Birim={colUnit}");

                if (colProductName == -1)
                {
                    Console.WriteLine("Ürün Adı sütunu bulunamadı");
                    return new List<Produce>();
                }

                // Başlık satırından sonraki satırlardan başla
                for (int i = headerRowIndex + 1; i < dataRows.Count; i++)
                {
                    try
                    {
                        var cells = dataRows[i].SelectNodes(".//td");
                        if (cells == null || cells.Count <= colProductName)
                        {
                            if (i - headerRowIndex <= 3) Console.WriteLine($"Satır {i - headerRowIndex}: Yeterli sütun yok ({cells?.Count ?? 0} sütun)");
                            continue;
                        }

                        var productName = cells[colProductName].InnerText?.Trim() ?? "";
                        if (string.IsNullOrEmpty(productName))
                        {
                            if (i - headerRowIndex <= 3) Console.WriteLine($"Satır {i - headerRowIndex}: Ürün adı boş");
                            continue;
                        }

                        var productType = colProductType >= 0 && cells.Count > colProductType ? cells[colProductType].InnerText?.Trim() : "";
                        // Excel'de sütun kayması var:
                        // - "Minimum Fiyat" sütunu = Aslında Ortalama Fiyat (bunu kullanıyoruz)
                        // - "Maksimum Fiyat" sütunu = Aslında İşlem Hacmi
                        // - "Ortalama Fiyat" sütunu = Aslında Birim Adı
                        var minPriceText = colMinPrice >= 0 && cells.Count > colMinPrice ? cells[colMinPrice].InnerText?.Trim() : "";
                        var volumeText = colMaxPrice >= 0 && cells.Count > colMaxPrice ? cells[colMaxPrice].InnerText?.Trim() : ""; // Maksimum Fiyat = İşlem Hacmi
                        var unit = colAveragePrice >= 0 && cells.Count > colAveragePrice ? cells[colAveragePrice].InnerText?.Trim() : ""; // Ortalama Fiyat = Birim Adı
                        
                        // İlk birkaç satırı detaylı logla
                        if (i - headerRowIndex <= 3)
                        {
                            Console.WriteLine($"Satır {i - headerRowIndex} parse: Ürün='{productName}', Cins='{productType}'");
                            Console.WriteLine($"  Tüm hücreler: {string.Join(" | ", cells.Select((c, idx) => $"[{idx}]={c.InnerText?.Trim()}"))}");
                            Console.WriteLine($"  Ortalama fiyat (Min sütunu, sütun {colMinPrice}): '{minPriceText}'");
                        }

                        // HTML entity'leri decode et
                        productName = System.Net.WebUtility.HtmlDecode(productName);
                        productType = System.Net.WebUtility.HtmlDecode(productType);

                        // Fiyat parse et
                        // Öncelik sırası: Ortalama Fiyat > (Min + Max) / 2 > Min > Max
                        decimal price = 0;
                        bool priceParsed = false;

                        // Fiyat parse helper fonksiyonu
                        decimal? ParsePrice(string priceText)
                        {
                            if (string.IsNullOrEmpty(priceText)) return null;
                            
                            var priceClean = priceText.Trim();
                            
                            // Türkçe formatını tespit et: hem nokta hem virgül varsa
                            var dotCount = priceClean.Count(c => c == '.');
                            var commaCount = priceClean.Count(c => c == ',');
                            
                            if (dotCount > 0 && commaCount > 0)
                            {
                                // Türkçe format: 75.706,99 -> nokta binlik, virgül ondalık
                                priceClean = priceClean.Replace(".", "").Replace(",", ".");
                            }
                            else if (commaCount > 0 && dotCount == 0)
                            {
                                // Sadece virgül var, ondalık ayırıcı (49,54 -> 49.54)
                                priceClean = priceClean.Replace(",", ".");
                            }
                            else if (dotCount > 0 && commaCount == 0)
                            {
                                // Sadece nokta var, binlik ayırıcı olabilir veya ondalık
                                // Son noktadan sonra 2-3 karakter varsa ondalık, yoksa binlik
                                var lastDotIndex = priceClean.LastIndexOf('.');
                                if (lastDotIndex >= 0 && lastDotIndex < priceClean.Length - 4)
                                {
                                    // Binlik ayırıcı, kaldır
                                    priceClean = priceClean.Replace(".", "");
                                }
                                // Aksi halde ondalık ayırıcı olarak kalır
                            }
                            else
                            {
                                // Sadece rakamlar, olduğu gibi
                                priceClean = Regex.Replace(priceClean, @"[^\d.]", "");
                            }
                            
                            if (decimal.TryParse(priceClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                            {
                                return parsedPrice;
                            }
                            
                            return null;
                        }

                        // Excel'de sütun kayması var: "Minimum Fiyat" sütunu aslında ortalama fiyatı içeriyor
                        // Sadece Minimum Fiyat sütununu kullan, Ortalama ve Maksimum'u atla
                        if (!string.IsNullOrEmpty(minPriceText))
                        {
                            var minPrice = ParsePrice(minPriceText);
                            if (minPrice.HasValue)
                            {
                                price = minPrice.Value;
                                priceParsed = true;
                                
                                if (i - headerRowIndex <= 3)
                                {
                                    Console.WriteLine($"  Fiyat parse (Minimum sütunu = Ortalama): '{minPriceText}' -> {minPrice.Value}");
                                }
                            }
                        }

                        // Hacim parse et (Maksimum Fiyat sütunu = İşlem Hacmi)
                        decimal volume = 0;
                        if (!string.IsNullOrEmpty(volumeText))
                        {
                            var volumeClean = Regex.Replace(volumeText, @"[^\d,.]", "").Replace(",", ".");
                            decimal.TryParse(volumeClean, NumberStyles.Any, CultureInfo.InvariantCulture, out volume);
                        }

                        if (priceParsed && price > 0)
                        {
                            produces.Add(new Produce
                            {
                                ProductName = productName,
                                ProductType = productType,
                                AveragePrice = price,
                                Volume = volume,
                                Unit = unit
                            });
                        }
                        else
                        {
                            if (i - headerRowIndex <= 3) Console.WriteLine($"Satır {i - headerRowIndex}: Fiyat parse edilemedi (minPriceText='{minPriceText}')");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (i - headerRowIndex <= 5) // İlk birkaç satır için log
                        {
                            Console.WriteLine($"Satır {i - headerRowIndex} parse edilemedi: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"HTML tablosundan {produces.Count} ürün parse edildi");
                if (produces.Count > 0)
                {
                    Console.WriteLine($"Örnek ürünler: {string.Join(", ", produces.Take(5).Select(p => p.ProductName))}");
                }
                return produces;
            }
            else
            {
                // Modern XLSX formatı - EPPlus ile parse et
                try
                {
                    using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
                    {
                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            Console.WriteLine("Excel dosyasında worksheet bulunamadı");
                            return new List<Produce>();
                        }

                        var worksheet = package.Workbook.Worksheets[0];
                        var produces = new List<Produce>();
                        var dimension = worksheet.Dimension;
                        
                        if (dimension == null) return new List<Produce>();
                        
                        for (int row = 2; row <= dimension.End.Row; row++)
                        {
                            var productName = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                            if (string.IsNullOrEmpty(productName)) continue;

                            var productType = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                            var priceText = worksheet.Cells[row, 4].Text?.Trim() ?? "";
                            var volumeText = worksheet.Cells[row, 5].Text?.Trim() ?? "";

                            priceText = Regex.Replace(priceText, @"[^\d,.]", "").Replace(",", ".");
                            volumeText = Regex.Replace(volumeText, @"[^\d,.]", "").Replace(",", ".");

                            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                                decimal.TryParse(volumeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                            {
                                produces.Add(new Produce
                                {
                                    ProductName = productName,
                                    ProductType = productType,
                                    AveragePrice = price,
                                    Volume = volume
                                });
                            }
                        }

                        Console.WriteLine($"XLSX'den {produces.Count} ürün parse edildi");
                        return produces;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"XLSX parse hatası: {ex.Message}");
                    return new List<Produce>();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excel indirme/parse hatası: {ex.Message}");
            return new List<Produce>();
        }
    }

    private async Task<List<Produce>> GetProduceListFromHtmlAsync()
    {
        try
        {
            var allProduces = new List<Produce>();
            
            // İlk sayfayı GET ile çek
            var firstPageHtml = await _httpClient.GetStringAsync(HalUrl);
            var firstPageDoc = new HtmlDocument();
            firstPageDoc.LoadHtml(firstPageHtml);

            // ViewState ve diğer form verilerini al
            ExtractFormData(firstPageDoc);
            
            // İlk sayfadaki verileri parse et
            var firstPageProduces = ParseTable(firstPageDoc);
            allProduces.AddRange(firstPageProduces);

            // Toplam sayfa sayısını bul
            int initialTotalPages = GetTotalPages(firstPageDoc);
            Console.WriteLine($"İlk sayfada bulunan maksimum sayfa: {initialTotalPages}");
            Console.WriteLine($"İlk sayfadan {firstPageProduces.Count} ürün çekildi");
            
            // İlk sayfadaki ürün adlarını kaydet (test için)
            var firstPageProductNames = firstPageProduces.Select(p => p.ProductName).Distinct().ToList();
            Console.WriteLine($"İlk sayfada {firstPageProductNames.Count} benzersiz ürün: {string.Join(", ", firstPageProductNames.Take(5))}...");

            // Toplam sayfa sayısını belirle - eğer 10 veya daha az ise, 25'e kadar test et
            int totalPages = initialTotalPages;
            if (initialTotalPages <= 10)
            {
                Console.WriteLine("10 veya daha az sayfa bulundu, son sayfalar test ediliyor...");
                // ViewState'i yedekle
                var savedViewState = _viewState;
                var savedViewStateGen = _viewStateGenerator;
                var savedEventValidation = _eventValidation;
                
                for (int testPage = initialTotalPages + 1; testPage <= 25; testPage++)
                {
                    try
                    {
                        var testProduces = await GetPageAsync(testPage);
                        if (testProduces != null && testProduces.Any())
                        {
                            // Farklı ürünler var mı kontrol et
                            var testProductNames = testProduces.Select(p => p.ProductName).Distinct().ToList();
                            if (!testProductNames.SequenceEqual(firstPageProductNames))
                            {
                                totalPages = testPage;
                                Console.WriteLine($"Sayfa {testPage} bulundu (farklı ürünler), yeni maksimum: {totalPages}");
                            }
                            else
                            {
                                Console.WriteLine($"Sayfa {testPage} aynı ürünleri içeriyor, durduruluyor");
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Sayfa {testPage} boş, toplam sayfa: {totalPages}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sayfa {testPage} test edilirken hata: {ex.Message}");
                        break;
                    }
                }
                
                // ViewState'i geri yükle (test sayfaları ViewState'i değiştirdi)
                _viewState = savedViewState;
                _viewStateGenerator = savedViewStateGen;
                _eventValidation = savedEventValidation;
                Console.WriteLine("ViewState geri yüklendi, asıl sayfalar çekilecek");
            }
            
            Console.WriteLine($"Toplam sayfa sayısı: {totalPages}");

            // Her sayfayı çek (2'den başla çünkü 1. sayfayı zaten çektik)
            if (totalPages > 1)
            {
                for (int page = 2; page <= totalPages; page++)
                {
                    try
                    {
                        var pageProduces = await GetPageAsync(page);
                        
                        // Sayfa gerçekten değişti mi kontrol et
                        var pageProductNames = pageProduces.Select(p => p.ProductName).Distinct().ToList();
                        if (pageProductNames.SequenceEqual(firstPageProductNames) && page > 2)
                        {
                            Console.WriteLine($"UYARI: Sayfa {page} aynı ürünleri içeriyor! ViewState güncellenmemiş olabilir.");
                        }
                        
                        allProduces.AddRange(pageProduces);
                        Console.WriteLine($"Sayfa {page}/{totalPages} çekildi. {pageProduces.Count} ürün eklendi. Toplam: {allProduces.Count}");
                        
                        // Rate limiting - her istek arasında kısa bir bekleme
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Sayfa {page} çekilirken hata: {ex.Message}");
                        // Hata olsa bile devam et
                    }
                }
            }
            else
            {
                Console.WriteLine("Sadece 1 sayfa var, diğer sayfalar çekilmeyecek");
            }

            Console.WriteLine($"Toplam {allProduces.Count} ürün çekildi");
            return allProduces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HAL servisinden veri çekilirken hata: {ex.Message}");
            return new List<Produce>();
        }
    }

    private void ExtractFormData(HtmlDocument doc)
    {
        // ViewState'i bul
        var viewStateNode = doc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']");
        _viewState = viewStateNode?.GetAttributeValue("value", "");

        // ViewStateGenerator'ı bul
        var viewStateGenNode = doc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEGENERATOR']");
        _viewStateGenerator = viewStateGenNode?.GetAttributeValue("value", "");

        // EventValidation'ı bul
        var eventValidationNode = doc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']");
        _eventValidation = eventValidationNode?.GetAttributeValue("value", "");

        // Table ID'yi bul
        var table = doc.DocumentNode.SelectSingleNode("//table[contains(@id, 'gvFiyatlar')]");
        _tableId = table?.GetAttributeValue("id", "");
    }

    private int GetTotalPages(HtmlDocument doc)
    {
        // Önce pagination satırını bul
        var paginationRow = doc.DocumentNode.SelectSingleNode("//tr[.//a[contains(@href, 'Page$') or contains(@onclick, 'Page$')]]");
        if (paginationRow == null)
        {
            Console.WriteLine("Pagination satırı bulunamadı");
            return 1;
        }

        var pageLinks = paginationRow.SelectNodes(".//a[contains(@href, 'Page$') or contains(@onclick, 'Page$')]");
        if (pageLinks == null || !pageLinks.Any())
        {
            Console.WriteLine("Sayfa linkleri bulunamadı");
            return 1;
        }

        int maxPage = 1;
        var pageNumbers = new HashSet<int>();

        foreach (var link in pageLinks)
        {
            // Href'ten sayfa numarasını bul
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
                var match = Regex.Match(href, @"Page\$(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNum))
                {
                    pageNumbers.Add(pageNum);
                    if (pageNum > maxPage)
                        maxPage = pageNum;
                }
            }

            // Onclick'ten sayfa numarasını bul
            var onclick = link.GetAttributeValue("onclick", "");
            if (!string.IsNullOrEmpty(onclick))
            {
                var match = Regex.Match(onclick, @"Page\$(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int pageNum))
                {
                    pageNumbers.Add(pageNum);
                    if (pageNum > maxPage)
                        maxPage = pageNum;
                }
            }

            // Link text'inden de sayfa numarasını bul (1, 2, 3 gibi)
            var linkText = link.InnerText.Trim();
            if (int.TryParse(linkText, out int textPageNum) && textPageNum > 0)
            {
                pageNumbers.Add(textPageNum);
                if (textPageNum > maxPage)
                    maxPage = textPageNum;
            }
        }

        Console.WriteLine($"İlk sayfada bulunan sayfa numaraları: {string.Join(", ", pageNumbers.OrderBy(x => x))}");
        Console.WriteLine($"İlk kontrol: Maksimum sayfa {maxPage}");

        // Eğer maksimum sayfa 10 veya daha az ise ve "..." (ellipsis) varsa,
        // muhtemelen daha fazla sayfa var. Ama bunu GetProduceListAsync içinde kontrol edeceğiz
        // Şimdilik bulduğumuz maksimum sayfayı döndür, sonra dinamik olarak artıracağız

        return maxPage;
    }
    

    private async Task<List<Produce>> GetPageAsync(int pageNumber)
    {
        if (string.IsNullOrEmpty(_tableId) || string.IsNullOrEmpty(_viewState))
        {
            Console.WriteLine($"Sayfa {pageNumber} için gerekli veriler eksik. TableId: {_tableId}, ViewState: {(_viewState != null ? "Var" : "Yok")}");
            return new List<Produce>();
        }

        // ASP.NET GridView pagination için POST verisi
        // __EVENTTARGET formatı: ctl00$ctl37$g_7e86b8d6_3aea_47cf_b1c1_939799a091e0$gvFiyatlar
        var eventTarget = _tableId.Replace("_", "$");
        
        var postData = new Dictionary<string, string>
        {
            { "__VIEWSTATE", _viewState ?? "" },
            { "__VIEWSTATEGENERATOR", _viewStateGenerator ?? "" },
            { "__EVENTVALIDATION", _eventValidation ?? "" },
            { "__EVENTTARGET", eventTarget },
            { "__EVENTARGUMENT", $"Page${pageNumber}" }
        };

        Console.WriteLine($"Sayfa {pageNumber} çekiliyor... EventTarget: {eventTarget}");

        // Cookie'leri korumak için HttpRequestMessage kullan
        var request = new HttpRequestMessage(HttpMethod.Post, HalUrl)
        {
            Content = new FormUrlEncodedContent(postData)
        };
        request.Headers.Add("Referer", HalUrl);
        
        var response = await _httpClient.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        // Response'u kontrol et
        if (html.Length < 1000)
        {
            Console.WriteLine($"Sayfa {pageNumber} için yanıt çok kısa: {html.Length} karakter");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // ViewState'i güncelle (her sayfada değişir) - ÖNEMLİ!
        var oldViewState = _viewState != null ? _viewState.Substring(0, Math.Min(50, _viewState.Length)) : "";
        ExtractFormData(doc);
        var newViewState = _viewState != null ? _viewState.Substring(0, Math.Min(50, _viewState.Length)) : "";
        
        if (oldViewState == newViewState && pageNumber > 2)
        {
            Console.WriteLine($"UYARI: Sayfa {pageNumber} için ViewState değişmedi! Aynı sayfa dönüyor olabilir.");
        }

        var produces = ParseTable(doc);
        
        // İlk birkaç ürün adını logla
        var sampleProducts = produces.Take(3).Select(p => p.ProductName).ToList();
        Console.WriteLine($"Sayfa {pageNumber}'dan {produces.Count} ürün çekildi. Örnekler: {string.Join(", ", sampleProducts)}");

        return produces;
    }

    private List<Produce> ParseTable(HtmlDocument doc)
    {
        var produces = new List<Produce>();
        
        var table = doc.DocumentNode.SelectSingleNode("//table[contains(@id, 'gvFiyatlar')]");
        
        if (table == null)
        {
            // Alternatif: Tüm tablolarda ara
            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables != null)
            {
                foreach (var t in tables)
                {
                    var rows = t.SelectNodes(".//tr");
                    if (rows != null && rows.Count > 1)
                    {
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var cells = rows[i].SelectNodes(".//td");
                            if (cells != null && cells.Count >= 6)
                            {
                                var produce = ParseRow(cells);
                                if (produce != null)
                                {
                                    produces.Add(produce);
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            var rows = table.SelectNodes(".//tr");
            if (rows != null)
            {
                // İlk satır başlık, ikinci satırdan başla
                // Son satır pagination olabilir, onu atla
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    // Pagination satırını atla (içinde "Page$" linki varsa)
                    if (row.SelectSingleNode(".//a[contains(@href, 'Page$')]") != null)
                        continue;

                    var cells = row.SelectNodes(".//td");
                    if (cells != null && cells.Count >= 6)
                    {
                        var produce = ParseRow(cells);
                        if (produce != null)
                        {
                            produces.Add(produce);
                        }
                    }
                }
            }
        }

        return produces;
    }

    private Produce? ParseRow(HtmlNodeCollection cells)
    {
        try
        {
            var productName = cells[0].InnerText.Trim();
            var productType = cells[1].InnerText.Trim();
            var productCategory = cells[2].InnerText.Trim();
            var priceText = cells[3].InnerText.Trim();
            var volumeText = cells[4].InnerText.Trim();
            var unit = cells[5].InnerText.Trim();

            // Fiyat ve hacim parse et
            priceText = Regex.Replace(priceText, @"[^\d,.]", "");
            priceText = priceText.Replace(",", ".");
            
            volumeText = Regex.Replace(volumeText, @"[^\d,.]", "");
            volumeText = volumeText.Replace(",", ".");

            if (decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                decimal.TryParse(volumeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
            {
                return new Produce
                {
                    ProductName = productName,
                    ProductType = productType,
                    ProductCategory = productCategory,
                    AveragePrice = price,
                    Volume = volume,
                    Unit = unit
                };
            }
        }
        catch
        {
            // Parse hatası, null döndür
        }

        return null;
    }

    public async Task<Produce?> GetProducePriceAsync(string productName, string? productType = null)
    {
        var produces = await GetProduceListAsync();
        
        // Ürün adına göre filtrele (case-insensitive, Türkçe karakter desteği)
        var matches = produces.Where(p => 
            p.ProductName.Equals(productName, StringComparison.OrdinalIgnoreCase) ||
            p.ProductName.Contains(productName, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (!matches.Any())
            return null;

        // Eğer tür belirtilmişse, türe göre filtrele
        if (!string.IsNullOrWhiteSpace(productType))
        {
            var typeMatch = matches.FirstOrDefault(p => 
                p.ProductType.Equals(productType, StringComparison.OrdinalIgnoreCase) ||
                p.ProductType.Contains(productType, StringComparison.OrdinalIgnoreCase)
            );
            
            if (typeMatch != null)
                return typeMatch;
        }

        // Geleneksel/Konvansiyonel öncelikli, yoksa ilk eşleşeni döndür
        var traditional = matches.FirstOrDefault(p => 
            p.ProductCategory.Contains("Geleneksel", StringComparison.OrdinalIgnoreCase) ||
            p.ProductCategory.Contains("Konvansiyonel", StringComparison.OrdinalIgnoreCase)
        );

        return traditional ?? matches.FirstOrDefault();
    }

    public async Task<ProducePriceComparison> ComparePriceAsync(string productName, decimal userPrice, string? productType = null)
    {
        var marketProduce = await GetProducePriceAsync(productName, productType);
        
        if (marketProduce == null)
        {
            return new ProducePriceComparison
            {
                UserPrice = userPrice,
                MarketPrice = 0,
                Result = "Bilinmiyor",
                Difference = 0,
                DifferencePercentage = 0
            };
        }

        var marketPrice = marketProduce.AveragePrice;
        var difference = userPrice - marketPrice;
        var differencePercentage = marketPrice > 0 ? (difference / marketPrice) * 100 : 0;

        string result;
        
        // %15 tolerans ile karşılaştırma
        if (differencePercentage <= -15)
            result = "Ucuz";
        else if (differencePercentage >= 15)
            result = "Pahalı";
        else
            result = "Normal";

        return new ProducePriceComparison
        {
            UserPrice = userPrice,
            MarketPrice = marketPrice,
            Result = result,
            Difference = difference,
            DifferencePercentage = differencePercentage,
            ProduceInfo = marketProduce
        };
    }
}

