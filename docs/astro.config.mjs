// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";

export default defineConfig({
	site: "https://mregni.github.io",
	base: "/BoardGameTracker",
	redirects: {
		"/documentation": "/BoardGameTracker/",
		"/documentation/getting-started": "/BoardGameTracker/getting-started/quick-start/",
		"/documentation/user-guide": "/BoardGameTracker/user-guide/",
		"/documentation/extra": "/BoardGameTracker/extra/development/",
	},
	integrations: [
		starlight({
			title: "BoardGameTracker",
			description:
				"Self-hosted board game collection, play-session and statistics tracker.",
			favicon: "/favicon.svg",
			social: [
				{
					icon: "github",
					label: "GitHub",
					href: "https://github.com/mregni/BoardGameTracker",
				},
			],
			customCss: [
				"@fontsource/chakra-petch/400.css",
				"@fontsource/chakra-petch/600.css",
				"@fontsource/chakra-petch/700.css",
				"./src/styles/custom.css",
			],
			editLink: {
				baseUrl: "https://github.com/mregni/BoardGameTracker/edit/master/docs/",
			},
			sidebar: [
				{
					label: "Getting Started",
					items: [
						"getting-started/quick-start",
						"getting-started/docker",
						"getting-started/environment-variables",
						"getting-started/email",
						"getting-started/proxy",
					],
				},
				{
					label: "User Guide",
					items: ["user-guide"],
				},
				{
					label: "Extra",
					items: [
						"extra/development",
						"extra/translations",
						"extra/bugs-features",
						"extra/logging",
					],
				},
			],
		}),
	],
});
