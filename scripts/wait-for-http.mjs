const [url, expectedStatusText = "200", timeoutText = "60000"] = process.argv.slice(2);

if (!url) {
  throw new Error("Usage: node scripts/wait-for-http.mjs <url> [statusCsv] [timeoutMs]");
}

const expectedStatuses = new Set(
  expectedStatusText
    .split(",")
    .map(value => Number.parseInt(value.trim(), 10))
    .filter(Number.isFinite)
);

if (expectedStatuses.size === 0) {
  throw new Error(
    `No valid expected HTTP statuses parsed from "${expectedStatusText}". Provide at least one numeric status code.`
  );
}

const timeoutMs = Number.parseInt(timeoutText, 10);
const startedAt = Date.now();
let lastStatus = null;
let lastError = null;

while (Date.now() - startedAt < timeoutMs) {
  try {
    const response = await fetch(url);
    lastStatus = response.status;
    lastError = null;

    if (expectedStatuses.has(response.status)) {
      process.stdout.write(`${response.status}\n`);
      process.exit(0);
    }
  } catch (error) {
    lastError = error;
  }

  await new Promise(resolve => setTimeout(resolve, 1_000));
}

let timeoutMessage = `Timed out waiting for ${url} to return one of [${[...expectedStatuses].join(", ")}].`;
if (lastStatus !== null) {
  timeoutMessage += ` Last HTTP status received: ${lastStatus}.`;
}
if (lastError !== null) {
  const errorMessage = lastError instanceof Error ? lastError.message : String(lastError);
  timeoutMessage += ` Last error: ${errorMessage}.`;
}

throw new Error(timeoutMessage);
