import { formOptions } from "@tanstack/react-form";

export const settingsFormOpts = formOptions({
	defaultValues: {
		uiLanguage: "",
		dateFormat: "",
		timeFormat: "",
		currency: "",
		statistics: false,
		updateCheckEnabled: false,
		versionTrack: "",
		shelfOfShameEnabled: false,
		shelfOfShameMonthsLimit: 0,
		publicUrl: "",
		gameNightsEnabled: false,
	},
});
