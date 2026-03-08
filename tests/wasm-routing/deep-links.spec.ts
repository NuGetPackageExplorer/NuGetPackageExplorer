import { expect, test } from "@playwright/test";

const stablePackage = {
  id: "Newtonsoft.Json",
  version: "13.0.3"
};

const previewPackage = {
  id: "Microsoft.Extensions.DependencyInjection",
  version: "11.0.0-preview.1.26104.118"
};

async function captureStartupSignals(page: import("@playwright/test").Page) {
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

async function enableUnoAccessibility(page: import("@playwright/test").Page) {
  await page.evaluate(() => {
    document.getElementById("uno-enable-accessibility")?.dispatchEvent(
      new MouseEvent("click", { bubbles: true })
    );
  });
}

async function getFirstSemanticPackageLabelId(page: import("@playwright/test").Page) {
  return await page.evaluate(() => {
    const ignoredLabels = new Set([
      "File",
      "uno",
      "Show pre-releases"
    ]);

    const candidates = Array.from(
      document.querySelectorAll<HTMLElement>("#uno-semantics-root [tabindex='0'][aria-label]")
    )
      .filter(element => {
        const label = element.getAttribute("aria-label") ?? "";

        return label.length > 0
          && !ignoredLabels.has(label)
          && !label.startsWith("by ")
          && !label.startsWith("v")
          && !/^[\\uE000-\\uF8FF0-9]+$/u.test(label);
      })
      .map(element => element.id);

    return candidates[0] ?? null;
  });
}

function expectNoStartupFailure(consoleMessages: string[]) {
  expect(
    consoleMessages.filter(message => message.includes("landing navigation failed")),
    "startup should not fail and fall back to the packages page"
  ).toEqual([]);
}

test("direct versioned deep link opens the requested package", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${stablePackage.id.replace(".", "\\.")}/${stablePackage.version}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${stablePackage.id.replace(".", "\\.")} ${stablePackage.version} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("direct deep link without a version resolves to a package view", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${stablePackage.id.replace(".", "\\.")}/[^/?#]+$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${stablePackage.id.replace(".", "\\.")} .*\\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("preview-version deep links keep the requested preview package open", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);

  await page.goto(`/packages/${previewPackage.id}/${previewPackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${previewPackage.id.replaceAll(".", "\\.")}/${previewPackage.version.replaceAll(".", "\\.")}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${previewPackage.id.replaceAll(".", "\\.")} ${previewPackage.version.replaceAll(".", "\\.")} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("pasted versioned deep links keep the requested package open", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);
  const targetPath = `/packages/${stablePackage.id}/${stablePackage.version}`;

  await page.goto("/packages", {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  const navigation = page.waitForURL(/\/packages(?:\/.*)?$/, {
    timeout: 30_000
  });

  await page.evaluate(path => {
    window.location.assign(path);
  }, targetPath);
  await navigation;
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${stablePackage.id.replace(".", "\\.")}/${stablePackage.version}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${stablePackage.id.replace(".", "\\.")} ${stablePackage.version} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("hard refresh on a versioned package deep link keeps the requested package open", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);

  await page.goto(`/packages/${stablePackage.id}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await page.reload({ waitUntil: "domcontentloaded" });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(
    new RegExp(`/packages/${stablePackage.id.replace(".", "\\.")}/${stablePackage.version}$`)
  );
  await expect(page).toHaveTitle(
    new RegExp(`^${stablePackage.id.replace(".", "\\.")} ${stablePackage.version} \\| NuGet Package Explorer$`)
  );
  expectNoStartupFailure(consoleMessages);
});

test("encoded package ids still resolve to the package view", async ({ page }) => {
  const consoleMessages = await captureStartupSignals(page);

  await page.goto(`/packages/${encodeURIComponent(stablePackage.id)}/${stablePackage.version}`, {
    waitUntil: "domcontentloaded"
  });
  await waitForUnoShell(page);

  await expect(page).toHaveURL(new RegExp(`/packages/${stablePackage.id.replace(".", "\\.")}/${stablePackage.version}$`));
  await expect(page).toHaveTitle(new RegExp(`^${stablePackage.id.replace(".", "\\.")} ${stablePackage.version} \\| NuGet Package Explorer$`));
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
