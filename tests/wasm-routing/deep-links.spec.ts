import fs from "node:fs";
import path from "node:path";
import { expect, test } from "@playwright/test";

const stablePackage = {
  id: "Newtonsoft.Json",
  version: "13.0.3"
};

const previewPackage = {
  id: "Microsoft.Extensions.DependencyInjection",
  version: "11.0.0-preview.1.26104.118"
};

const wasmPublishIndexPath = path.resolve(
  process.cwd(),
  "artifacts/publish/NuGetPackageExplorer.WinUI/release_net10.0-browserwasm/wwwroot/index.html"
);

let publishedPackageBasePath: string | undefined;

function escapeRegex(text: string) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function getPublishedPackageBasePath() {
  publishedPackageBasePath ??= (() => {
    const indexHtml = fs.readFileSync(wasmPublishIndexPath, "utf8");
    const packagePathMatch = indexHtml.match(/\/(package_[^/]+)\/uno-bootstrap\.js/);

    if (!packagePathMatch) {
      throw new Error(`Could not find a published package base path in ${wasmPublishIndexPath}.`);
    }

    return packagePathMatch[1];
  })();

  return publishedPackageBasePath;
}

function captureStartupSignals(page: import("@playwright/test").Page) {
  const consoleMessages: string[] = [];

  page.on("console", message => {
    consoleMessages.push(message.text());
  });

  return consoleMessages;
}

async function waitForUnoShell(page: import("@playwright/test").Page) {
  await expect.poll(async () => {
    try {
      return await page.title();
    } catch {
      return "";
    }
  }, {
    timeout: 30_000
  }).not.toBe("");
}

function expectNoStartupFailure(consoleMessages: string[]) {
  expect(
    consoleMessages.filter(message => message.includes("landing navigation failed")),
    "startup should not fail and fall back to the packages page"
  ).toEqual([]);
}

test("direct versioned deep link opens the requested package", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(stablePackage.id)}/${escapeRegex(stablePackage.version)}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(stablePackage.id)} ${escapeRegex(stablePackage.version)} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("direct deep link without a version resolves to a package view", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(stablePackage.id)}/[^/?#]+$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(stablePackage.id)} .*\\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("preview-version deep links keep the requested preview package open", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);

  await page.goto(`/packages/${previewPackage.id}/${previewPackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(previewPackage.id)}/${escapeRegex(previewPackage.version)}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(previewPackage.id)} ${escapeRegex(previewPackage.version)} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("pasted versioned deep links keep the requested package open", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);
  const targetPath = `/packages/${stablePackage.id}/${stablePackage.version}`;
  const targetUrlPattern = new RegExp(`/packages/${escapeRegex(stablePackage.id)}/${escapeRegex(stablePackage.version)}$`);

  await page.goto("/packages", {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await Promise.all([
    page.waitForURL(targetUrlPattern, {
      timeout: 30_000
    }),
    page.evaluate(path => {
      window.location.assign(path);
    }, targetPath)
  ]);
  await waitForUnoShell(page);

  await expect(page).toHaveURL(targetUrlPattern);
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(stablePackage.id)} ${escapeRegex(stablePackage.version)} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("hard refresh on a versioned package deep link keeps the requested package open", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await page.reload({ waitUntil: "domcontentloaded" });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(stablePackage.id)}/${escapeRegex(stablePackage.version)}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(stablePackage.id)} ${escapeRegex(stablePackage.version)} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("published assets do not leak package base paths into canonical deep links", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);
  const packageBasePath = getPublishedPackageBasePath();
  const targetPath = `/packages/${previewPackage.id}/${previewPackage.version}`;

  await page.goto(targetPath, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(previewPackage.id)}/${escapeRegex(previewPackage.version)}$`)
  );
  expect(page.url()).not.toContain(`/${packageBasePath}/`);
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(previewPackage.id)} ${escapeRegex(previewPackage.version)} \\| NuGet Package Explorer$`)
  );

  await page.reload({ waitUntil: "domcontentloaded" });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${escapeRegex(previewPackage.id)}/${escapeRegex(previewPackage.version)}$`)
  );
  expect(page.url()).not.toContain(`/${packageBasePath}/`);
  await expect(page).toHaveTitle(
    new RegExp(`^${escapeRegex(previewPackage.id)} ${escapeRegex(previewPackage.version)} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("encoded package ids still resolve to the package view", async ({ page }) => {
  const consoleMessages = captureStartupSignals(page);

  await page.goto(`/packages/${encodeURIComponent(stablePackage.id)}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(new RegExp(`/packages/${escapeRegex(stablePackage.id)}/${escapeRegex(stablePackage.version)}$`));
  await expect(page).toHaveTitle(new RegExp(`^${escapeRegex(stablePackage.id)} ${escapeRegex(stablePackage.version)} \\| NuGet Package Explorer$`));
  expectNoStartupFailure(consoleMessages);
});

test("search and invalid routes preserve their current behavior", async ({ page }) => {
  await page.goto("/packages?q=uno", {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);
  await expect(page).toHaveURL(/\/packages\?q=uno$/);
  await expect(page).toHaveTitle(/^Packages \| NuGet Package Explorer$/);

  await page.goto(`/packages/${stablePackage.id}/${stablePackage.version}/invalid`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);
  await expect(page).toHaveURL(/\/packages$/);
  await expect(page).toHaveTitle(/^Packages \| NuGet Package Explorer$/);
});
