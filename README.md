# wshcmx/net

[![Build](https://github.com/wshcmx/net/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/wshcmx/net/actions/workflows/build.yml)

Подключаемая .NET библиотека для работы в окружении WebSoft HCM. Предоставляет функции для выполнения SQL-запросов и рендеринга шаблонов, доступные из скриптового языка платформы.

## Возможности

- **Sql** — выполнение SQL-запросов, хранимых процедур и процедур с пагинацией к MS SQL Server
- **Templater** — рендеринг Mustache-шаблонов из строки или файла

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

### Templater

```js
// Рендеринг Mustache-шаблона (строка или путь к файлу)
Templater.Generate(template, data) // → string
```

## Структура проекта

| Проект | Описание |
|---|---|
| `src/wshcmx` | Основная библиотека |
| `src/Typifier` | CLI-утилита для генерации TypeScript-типов из .NET сборки |
| `src/Test` | Тесты |

## Сборка

```bash
dotnet build
```

## Генерация TypeScript-типов

```bash
dotnet build src/wshcmx
dotnet run --project src/Typifier -- --assembly src/wshcmx/bin/Debug/net10.0/wshcmx.dll --output types/wshcmx.d.ts
```