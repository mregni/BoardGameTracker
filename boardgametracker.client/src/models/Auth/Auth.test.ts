import { describe, expect, it } from "vitest";
import { ChangePasswordSchema, CreateUserSchema, ResetPasswordSchema } from "./Auth";

const message = "settings:account.password.min-length";

describe.each([
	["add user", CreateUserSchema.shape.password],
	["change password", ChangePasswordSchema.shape.newPassword],
	["reset password", ResetPasswordSchema.shape.newPassword],
])("password rule of %s", (_name, schema) => {
	it("refuses 7 characters", () => {
		expect(schema.safeParse("1234567").error?.issues[0].message).toBe(message);
	});

	it("accepts 8 characters", () => {
		expect(schema.safeParse("12345678").success).toBe(true);
	});
});
