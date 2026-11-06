import { expectComparison, openCompare } from "../support/comparePage";
import { test } from "../support/fixtures";
import { allPairs } from "../support/standings";

for (const [one, two] of allPairs) {
	test(`shows the true numbers for ${one} against ${two}`, async ({ page }) => {
		await openCompare(page, one, two);

		await expectComparison(page, one, two);
	});
}
