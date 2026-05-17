# wshcmx/net

[![Build](https://github.com/wshcmx/net/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/wshcmx/net/actions/workflows/build.yml)

Подключаемая .NET библиотека для работы в окружении WebSoft HCM. Предоставляет функции для выполнения SQL-запросов, рендеринга шаблонов и запуска внешних процессов, доступные из скриптового языка платформы.

## Возможности

- **Sql** — выполнение SQL-запросов, хранимых процедур и процедур с пагинацией для `SqlServer` и `PostgreSql`
- **Templater** — рендеринг Mustache-шаблонов из строки или файла
- **ProcessExecutor** — запуск внешних процессов с захватом stdout, stderr, кода завершения и времени выполнения
- **Typifier** — генерация TypeScript-типов для публичных классов сборки

## Подключение

### Через [wshcmx/lib](https://github.com/wshcmx/lib)

Библиотека входит в состав пакета `wshcmx/lib` и подключается автоматически.

### Вручную

```js
tools.dotnet_host?.Object.GetAssembly("wshcmx.dll")
```

## API

### Sql

```js
// Инициализация подключения
Sql.Init(connectionString)
Sql.Init(connectionString, DatabaseType.PostgreSql)
Sql.Init(connectionString, 1) // PostgreSql

// Выполнение произвольного SQL-запроса
Sql.ExecuteQuery(commandText) // → KeyValuePair<string, object>[][]

// Выполнение SQL без возврата данных (INSERT, UPDATE, DELETE)
Sql.ExecuteNonQuery(commandText)

// Вызов хранимой процедуры
Sql.ExecuteProcedure(procedureName, serializedParameters?) // → object[]

// Вызов хранимой процедуры с пагинацией
// options: { page, size, select, orderby }
Sql.ExecutePaginationProcedure(procedureName, serializedOptions, serializedParameters) // → [totalCount, rows]
```

Поддерживаемые значения `DatabaseType`:

- `SqlServer` — значение по умолчанию
- `PostgreSql`

### Templater

```js
// Рендеринг Mustache-шаблона (строка или путь к файлу)
Templater.Generate(template, data) // → string
```

### ProcessExecutor

```js
// Выполнение внешней команды
ProcessExecutor.Execute(command, arguments?, workingDirectory?, timeoutMilliseconds?) // → ProcessResult
```

`ProcessResult` содержит:

- `ExitCode`
- `Completed`
- `IsSuccess`
- `StandardOutput`
- `StandardError`
- `StartTime`
- `ExitTime`
- `Duration`

## Структура проекта

| Проект | Описание |
|---|---|
| `src/wshcmx` | Основная библиотека |
| `src/Typifier` | CLI-утилита для генерации TypeScript-типов в `src\Typifier\types\index.d.ts` |
| `src/Test` | Тесты |

## Сборка

```bash
dotnet build wshcmx.slnx
```

## Тесты

```bash
dotnet test wshcmx.slnx --no-logo
```

## Генерация TypeScript-типов

```bash
dotnet build wshcmx.slnx
dotnet run --project src\Typifier\Typifier.csproj
```

Сгенерированный файл сохраняется в `src\Typifier\types\index.d.ts`.
