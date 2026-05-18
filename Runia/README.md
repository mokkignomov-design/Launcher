Карта эндпоинтов (актуальная, вычитанная из кода)

Формат: название поинта | путь | что принимает | кратко — что делает | что отдает | политики/защита

---

Ниже перечислены эндпоинты, вычитанные по коду контроллеров в проекте. Указаны реальные DTO и политики (Authorize, RateLimit и т.д.).

## /api/auth

- POST /api/auth/confirm-email
  - Принимает: ConfirmCodeDto { Email, Code }
  - Что делает: Проверяет код подтверждения, помечает email подтверждённым и код использованным.
  - Возвращает: 200 { message } / 400
  - Политики: [EnableRateLimiting("LoginPolicy")]

- POST /api/auth/resend-code
  - Принимает: ResendCodeDto { Email }
  - Что делает: Удаляет старые коды, создаёт и отправляет новый код подтверждения (ответ не раскрывает существование email).
  - Возвращает: 200 { message }
  - Политики: [EnableRateLimiting("LoginPolicy")]

- POST /api/auth/request-password-reset
  - Принимает: PasswordResetRequestDto { Email }
  - Что делает: Генерирует код для сброса пароля и отправляет письмо (ответ не раскрывает наличие аккаунта).
  - Возвращает: 200 { message }
  - Политики: [EnableRateLimiting("LoginPolicy")]

- POST /api/auth/reset-password
  - Принимает: PasswordResetConfirmDto { Email, Code, NewPassword }
  - Что делает: Валидирует код сброса, проверяет силу пароля, обновляет хэш и удаляет refresh-токены.
  - Возвращает: 200 { message } / 400
  - Политики: [EnableRateLimiting("LoginPolicy")]

- POST /api/auth/register
  - Принимает: RegisterDto { Username, Password, Email }
  - Что делает: Создаёт пользователя, генерирует код подтверждения и отправляет письмо.
  - Возвращает: 200 { message }
  - Политики: [EnableRateLimiting("RegisterPolicy")]

- POST /api/auth/login
  - Принимает: LoginDto { Username, Password }
  - Что делает: Проверяет пароль, прогрессивная блокировка (IMemoryCache), создаёт access и refresh токены, сохраняет RefreshToken и UserSession.
  - Возвращает: 200 AuthResponseDto { AccessToken, RefreshToken } / 401 / 429
  - Политики: [EnableRateLimiting("LoginPolicy")]

- GET /api/auth/sessions
  - Принимает: JWT
  - Что делает: Возвращает список сессий текущего пользователя.
  - Возвращает: 200 [ { id, deviceInfo, ipAddress, createdAt, isCurrent } ]
  - Политики: [Authorize]

- DELETE /api/auth/sessions/{id}
  - Принимает: путь id, JWT
  - Что делает: Удаляет сессию и связанный refresh token при наличии.
  - Возвращает: 204 / 404
  - Политики: [Authorize]

- GET /api/auth/external-login?provider=Google
  - Что делает: Инициирует внешнюю аутентификацию (Challenge Google).
  - Возвращает: Challenge/redirect
  - Политики: публичный

- GET /api/auth/google-callback
  - Что делает: Обрабатывает callback от Google, создаёт/находит пользователя, генерирует токены и сессию.
  - Возвращает: 200 AuthResponseDto / 401
  - Политики: публичный

- GET /api/auth/me
  - Принимает: JWT
  - Что делает: Возвращает профиль пользователя (MeResponseDto), stats, skin URL, активности.
  - Возвращает: 200 MeResponseDto / 401 / 404
  - Политики: [Authorize]

- POST /api/auth/refresh
  - Принимает: RefreshTokenDto { RefreshToken }
  - Что делает: Валидирует и вращает refresh-token, выдаёт новый access и refresh.
  - Возвращает: 200 AuthResponseDto / 401
  - Политики: [EnableRateLimiting("LoginPolicy")]

---

## /api/users

- GET /api/users/{username}
  - Что делает: Возвращает публичный профиль пользователя (MeResponseDto) с абсолютным URL скина и активностями.
  - Возвращает: 200 MeResponseDto / 404
  - Политики: публичный

---

## /api/session

- GET /api/session/create
  - Принимает: JWT
  - Что делает: [Authorize] Генерирует temporaryKey (Base64 of 32 bytes), сохраняет в IMemoryCache под ключом session_tmpkey_{username.ToLowerInvariant()} с TTL = 18000 секунд (5 часов).
  - Возвращает: 200 SessionResponse { Username, TemporaryKey, ExpiresInSeconds }

- GET /api/session/verify?username={username}&temporaryKey={temporaryKey}
  - Принимает: query username, temporaryKey; заголовок X-Server-Key
  - Что делает: Проверяет X-Server-Key == _configuration["ServerApiKey"], затем ISessionService.VerifyTemporaryKey(username, temporaryKey). При успехе удаляет ключ и возвращает { status: "Confirmed" }.
  - Возвращает: 200 { status = "Confirmed" } / 401 / 403

- POST /api/session/verify-dedicated
  - Принимает: DedicatedServerVerifyRequest { Username, TemporaryKey, ServerDedicatedApiKey }
  - Что делает: [AllowAnonymous] Ожидаемый ключ читается из _configuration["DedicatedServerApiKey"] или используется fallback в коде. Если ключ неверен — 403 { error: "Access Denied: Invalid Server Key" }. Если верен — ISessionService.VerifyTemporaryKey(...). При успехе 200 { status = "Confirmed" }, иначе 401 { error: "Invalid session token or expired" }.
  - DTO запроса: DedicatedServerVerifyRequest

Примечание: temporaryKey хранится в IMemoryCache процесса — для корректной верификации важно, чтобы verify-запрос попадал на тот же инстанс приложения. Для кластерных развёртываний используйте IDistributedCache/Redis.

---

## /api/v1/skins

Контроллер помечен [Authorize]

- POST /api/v1/skins/upload
  - Принимает: multipart/form-data UploadSkinDto { IFormFile File, string? Username }
  - Что делает: Ограничение размера 2MB, проверка PNG header, сохранение в data/skins, создание записи Skin и пометка current skin.
  - Возвращает: 200 { url, hash } / 400 / 500

- GET /api/v1/skins/list
  - Принимает: JWT
  - Что делает: Возвращает список скинов текущего пользователя с абсолютными URL.
  - Возвращает: 200 [ { id, uuid, url, uploadedAt, isCurrent } ]

- GET /api/v1/skins/info/{username}
  - Принимает: путь username
  - Что делает: Возвращает url текущего скина и ETag (по времени файла). Поддерживается If-None-Match -> 304.
  - Возвращает: 200 { url, hash } / 304 / 404
  - Политики: [AllowAnonymous]

---

## /api/launcher

- GET /api/launcher/news
  - Что делает: Читает data/news.json (кеш 5 минут), возвращает топ-10.
  - Возвращает: 200 LauncherNewsResponse { News }

- GET /api/launcher/servers
  - Что делает: Читает data/servers.json (кеш 1 минуту), маппит в ServerResponseDto и возвращает активные серверы.
  - Возвращает: 200 LauncherServersResponse { Servers }

- POST /api/launcher/verify/{serverId}
  - Принимает: List<ArtifactDto>
  - Что делает: Сравнивает clientFiles с data/artifacts_{serverId}.json и возвращает toDownload/toDelete.
  - Возвращает: 200 { valid: bool, toDownload, toDelete }

- GET /api/launcher/artifacts/{serverId}
  - Что делает: Отдаёт data/artifacts_{serverId}.json как PhysicalFile (application/json) или 404.

---

## /api/admin (контроллер с [Authorize(Roles = "Admin")])

- POST /api/admin/artifacts/{serverId}
  - Принимает: List<ArtifactDto>
  - Что делает: Сохраняет data/artifacts_{serverId}.json
  - Возвращает: 200 { message }

- POST /api/admin/news
  - Принимает: List<object>
  - Что делает: Сохраняет data/news.json
  - Возвращает: 200 { message }

- POST /api/admin/servers
  - Принимает: object
  - Что делает: Сохраняет data/servers.json
  - Возвращает: 200 { message }

---
