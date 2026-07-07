# Runia Updater

Минимальный апдейтер для Runia (шаблон).

Как работает
- Пытается быстро пропинговать GitHub; если доступен — использует PrimaryBase, иначе переключается на FallbackBase.
- Ожидает JSON metadata по пути {base}/latest.json формата:
  {
	"assetUrl":"https://.../RuniaClient-1.2.3.exe",
	"sha256":"..."
  }
- Скачивает asset в %LocalAppData%\Runia\updates, проверяет SHA256.

Использование
- Скачивание + проверка (по-умолчанию):
  dotnet run --project Runia/Updater -- <assetUrl> <expectedSha>

- Применение загруженного обновления (замена и перезапуск):
  Runia.Updater.exe apply <sourceFile> <targetExe> [restartArgs]

CI / Secrets
- Для workflow требуется настроить секреты в GitHub:
  - AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, AWS_S3_BUCKET (если зеркалите в S3)
  - VPS_HOST, VPS_USER, VPS_SSH_KEY, VPS_PATH, VPS_PORT (если зеркалите на VPS)

Примечания
- Замените PrimaryBase и FallbackBase в Program.cs на реальные URL.
- Для безопасной цепочки проверяйте подпись/хэш вне апдейтера и используйте HTTPS.
