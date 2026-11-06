import { readFile } from "node:fs/promises";
import path from "node:path";
import type { Page } from "@playwright/test";
import { credentials, expect, paceLogin, readerCredentials, test, writerCredentials } from "../support/fixtures";
import { avatars, linkedPlayer, players } from "../support/seedData";
import {
	api,
	createUser,
	findUser,
	openSection,
	removeUser,
	sidebarLink,
	signInAs,
	submitLogin,
} from "../support/settings";

test.use({ allowedApiStatuses: [400, 401, 404] });
test.describe.configure({ timeout: 60_000 });

const field = (page: Page, name: string) => page.getByRole("textbox", { name, exact: true });
const saveProfile = (page: Page) => page.getByRole("button", { name: "Save profile" }).click();

const openPasswordDialog = async (page: Page) => {
	await page.getByRole("button", { name: "Change password" }).click();
	return page.getByRole("dialog");
};

const setEmail = async (page: Page, username: string, role: string, email: string | null) => {
	const user = await findUser(page, username);
	await (await api(page)).put(`/api/admin/users/${user?.id}`, { username, email, role, playerId: user?.playerId });
};

test("shows the own profile", async ({ page }) => {
	await openSection(page, "account");
	const main = page.getByRole("main");

	await expect(main).toContainText(new RegExp(String.raw`Username\s*${credentials.username}`));
	await expect(field(page, "Display name")).toHaveValue("Administrator");
	await expect(field(page, "Email")).toHaveValue("");
	await expect(page.getByRole("combobox", { name: "Linked player" })).toHaveText(linkedPlayer);
	await expect(field(page, "Current password")).toHaveCount(0);
	await expect(page.getByRole("button", { name: "Remove picture" })).toHaveCount(0);
});

test("changes the display name and shows it in the sidebar", async ({ page }) => {
	const name = "Grand master";
	try {
		await openSection(page, "account");
		await field(page, "Display name").fill(name);
		await saveProfile(page);

		await expect(page.getByText("Profile saved")).toBeVisible();
		await expect(page.getByRole("complementary")).toContainText(name);

		await openSection(page, "account");
		await expect(field(page, "Display name")).toHaveValue(name);
	} finally {
		await field(page, "Display name").fill("Administrator");
		await saveProfile(page);
		await expect(page.getByRole("complementary")).toContainText("Administrator");
	}
});

test("asks for the current password before changing the email", async ({ page }) => {
	const email = "admin@example.test";
	try {
		await openSection(page, "account");
		await field(page, "Email").fill(email);
		await expect(field(page, "Current password")).toBeVisible();

		await field(page, "Current password").fill("wrong-password");
		await saveProfile(page);
		await expect(page.getByText("Enter your current password to change your email address.")).toBeVisible();
		await openSection(page, "account");
		await expect(field(page, "Email")).toHaveValue("");

		await field(page, "Email").fill(email);
		await field(page, "Current password").fill(credentials.password);
		await saveProfile(page);
		await expect(page.getByText("Profile saved")).toBeVisible();

		await openSection(page, "account");
		await expect(field(page, "Email")).toHaveValue(email);
		await expect(field(page, "Current password")).toHaveCount(0);
	} finally {
		await setEmail(page, credentials.username, "Admin", null);
	}
});

test("clears the email", async ({ page, writerPage }) => {
	await openSection(writerPage, "account");
	const original = await field(writerPage, "Email").inputValue();
	expect(original).not.toBe("");

	try {
		await field(writerPage, "Email").fill("");
		await saveProfile(writerPage);
		await expect(writerPage.getByText("Profile saved")).toBeVisible();

		await openSection(writerPage, "account");
		test.fail(true, "BUG: clearing the email shows Profile saved but the old email stays");
		await expect(field(writerPage, "Email")).toHaveValue("");
	} finally {
		await setEmail(page, writerCredentials.username, writerCredentials.role, original);
	}
});

test("unlinks and links the own player", async ({ page }) => {
	const select = page.getByRole("combobox", { name: "Linked player" });
	try {
		await openSection(page, "account");
		await select.click();
		await page.getByRole("option", { name: "Not linked" }).click();
		await saveProfile(page);
		await expect(page.getByText("Profile saved")).toBeVisible();

		await openSection(page, "account");
		await expect(select).toHaveText("Not linked");
		await expect(
			page
				.getByRole("row", { name: /admin Admin/ })
				.getByRole("cell")
				.nth(2),
		).toHaveText("—");
	} finally {
		await select.click();
		await page.getByRole("option", { name: linkedPlayer, exact: true }).click();
		await saveProfile(page);
		await expect(
			page
				.getByRole("row", { name: /admin Admin/ })
				.getByRole("cell")
				.nth(2),
		).toHaveText(linkedPlayer);
	}
});

test("hides a player that belongs to someone else", async ({ writerPage }) => {
	await openSection(writerPage, "account");

	await writerPage.getByRole("combobox", { name: "Linked player" }).click();

	await expect(writerPage.getByRole("option")).toHaveText([
		"Not linked",
		...players.filter((x) => x !== linkedPlayer).sort((a, b) => a.localeCompare(b)),
	]);
});

test("uploads and removes a profile picture", async ({ page, writerPage }, testInfo) => {
	const [avatar] = avatars;
	const buffer = await readFile(path.join(testInfo.project.testDir, "support", "avatars", avatar.avatar));
	await openSection(writerPage, "account");
	const picture = writerPage.getByRole("main").getByRole("img", { name: writerCredentials.username });

	await writerPage.getByTestId("profile-picture-input").setInputFiles({
		name: avatar.avatar,
		mimeType: avatar.avatar.endsWith(".png") ? "image/png" : "image/jpeg",
		buffer,
	});

	await expect(writerPage.getByText("Profile picture saved")).toBeVisible();
	await expect(picture).toBeVisible();
	await expect(writerPage.getByRole("button", { name: "Remove picture" })).toBeVisible();

	await openSection(page, "account");
	await expect(page.getByRole("row", { name: /writer User/ }).getByRole("img")).toBeVisible();

	await openSection(writerPage, "account");
	await writerPage.getByRole("button", { name: "Remove picture" }).click();

	await expect(writerPage.getByText("Profile picture removed")).toBeVisible();
	await expect(writerPage.getByRole("button", { name: "Remove picture" })).toHaveCount(0);
});

test("refuses a file that is not a picture", async ({ writerPage }) => {
	await openSection(writerPage, "account");

	await writerPage.getByTestId("profile-picture-input").setInputFiles({
		name: "notes.txt",
		mimeType: "text/plain",
		buffer: Buffer.from("not a picture"),
	});

	await expect(writerPage.getByText("Unsupported image format")).toBeVisible();
	await expect(writerPage.getByRole("button", { name: "Remove picture" })).toHaveCount(0);
});

test("validates the password dialog and closes on cancel", async ({ page }) => {
	await openSection(page, "account");
	const dialog = await openPasswordDialog(page);
	const save = dialog.getByRole("button", { name: "Change password" });

	await expect(dialog.getByRole("heading", { name: "Change password" })).toBeVisible();
	await expect(dialog).toContainText("Enter your current password and choose a new one.");

	await save.click();
	await expect(dialog.getByText("Required", { exact: false })).toHaveCount(2);
	await expect(field(page, "Current password")).toBeFocused();

	await field(page, "New password").fill("abc");
	await expect(dialog.getByText(/^Password must be at least/)).toBeVisible();

	await field(page, "New password").fill("long-enough");
	await field(page, "Confirm new password").fill("different");
	await expect(dialog.getByText("Passwords do not match")).toBeVisible();

	await dialog.getByRole("button", { name: "Cancel" }).click();
	await expect(dialog).toBeHidden();

	await page.getByRole("button", { name: "Change password" }).click();
	await expect(field(page, "New password")).toHaveValue("");
});

test("says so when the current password is wrong", async ({ page }) => {
	await openSection(page, "account");
	const dialog = await openPasswordDialog(page);

	await field(page, "Current password").fill("wrong-password");
	await field(page, "New password").fill("another-password");
	await field(page, "Confirm new password").fill("another-password");
	await paceLogin();
	await dialog.getByRole("button", { name: "Change password" }).click();

	await expect(dialog.getByText("Incorrect password.")).toBeVisible();
	await expect(dialog).toBeVisible();
});

test("tells the password rule of the server", async ({ page }) => {
	await openSection(page, "account");
	const dialog = await openPasswordDialog(page);

	await field(page, "New password").fill("short");

	await expect(dialog.getByText("Password must be at least 8 characters")).toBeVisible();
});

test("changes the password, signs out old sessions and accepts only the new one", async ({
	page,
	browser,
	baseURL,
}) => {
	const username = "password-changer";
	const oldPassword = "old-password";
	const newPassword = "new-password";
	await createUser(page, username, oldPassword);
	const own = await signInAs(browser, baseURL, username, oldPassword);
	try {
		await openSection(own, "account");
		const dialog = await openPasswordDialog(own);
		await field(own, "Current password").fill(oldPassword);
		await field(own, "New password").fill(newPassword);
		await field(own, "Confirm new password").fill(newPassword);
		await paceLogin();
		await dialog.getByRole("button", { name: "Change password" }).click();

		await expect(own.getByText("Password changed")).toBeVisible();
		await expect(dialog).toBeHidden();

		await submitLogin(own, username, oldPassword);
		await expect(own.getByText("Invalid username or password")).toBeVisible();

		await submitLogin(own, username, newPassword);
		await expect(sidebarLink(own, "Settings")).toBeVisible();
	} finally {
		await own.context().close();
		await removeUser(page, username);
	}
});

test("lets a reader change the own display name", async ({ readerPage }) => {
	const name = "Quiet reader";
	try {
		await openSection(readerPage, "account");
		await field(readerPage, "Display name").fill(name);
		await saveProfile(readerPage);

		await expect(readerPage.getByText("Profile saved")).toBeVisible();
		await expect(readerPage.getByRole("complementary")).toContainText(name);
	} finally {
		await field(readerPage, "Display name").fill(readerCredentials.username);
		await saveProfile(readerPage);
		await expect(readerPage.getByRole("complementary")).toContainText(readerCredentials.username);
	}
});
