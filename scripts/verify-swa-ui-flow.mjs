import { chromium } from "@playwright/test";

const baseUrl = process.env.NPE_SWA_URL ?? "http://127.0.0.1:4280";
const previewPath = "/packages/Microsoft.Extensions.DependencyInjection/11.0.0-preview.1.26104.118";

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });
const consoleMessages = [];

page.on("console", message => {
  consoleMessages.push(message.text());
});

await page.goto(`${baseUrl}/packages?q=uno`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(8_000);
await page.evaluate(() => {
  document.getElementById("uno-enable-accessibility")?.dispatchEvent(
    new MouseEvent("click", { bubbles: true })
  );
});
await page.waitForTimeout(5_000);

const packageRow = page.locator('#uno-semantics-root [aria-label="NupkgExplorer.Client.Data.PackageData"]').first();
await packageRow.dblclick({ force: true });
await page.waitForTimeout(12_000);

const afterUiUrl = page.url();
const afterUiTitle = await page.title();

await page.goto(`${baseUrl}${previewPath}`, { waitUntil: "domcontentloaded" });
await page.waitForTimeout(12_000);

const finalUrl = page.url();
const finalTitle = await page.title();

await browser.close();

const startupFailures = consoleMessages.filter(message => message.includes("landing navigation failed"));

if (!/\/packages\/[^/]+\/[^/?#]+$/.test(afterUiUrl) || /Packages \| NuGet Package Explorer/.test(afterUiTitle)) {
  throw new Error(`Search UI open failed. url=${afterUiUrl} title=${afterUiTitle}`);
}

if (!finalUrl.endsWith(previewPath) || finalTitle !== "Microsoft.Extensions.DependencyInjection 11.0.0-preview.1.26104.118 | NuGet Package Explorer") {
  throw new Error(`Deep link after UI navigation failed. url=${finalUrl} title=${finalTitle}`);
}

if (startupFailures.length > 0) {
  throw new Error(`Unexpected startup failures observed:\n${startupFailures.join("\n---\n")}`);
}

console.log(JSON.stringify({
  afterUiUrl,
  afterUiTitle,
  finalUrl,
  finalTitle
}, null, 2));
