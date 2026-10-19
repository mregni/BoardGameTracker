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
			head: [
				{ tag: "link", attrs: { rel: "icon", href: "/BoardGameTracker/favicon.ico", sizes: "32x32" } },
				{ tag: "link", attrs: { rel: "icon", type: "image/png", sizes: "16x16", href: "/BoardGameTracker/favicon-16x16.png" } },
				{ tag: "link", attrs: { rel: "icon", type: "image/png", sizes: "32x32", href: "/BoardGameTracker/favicon-32x32.png" } },
				{ tag: "link", attrs: { rel: "icon", type: "image/png", sizes: "192x192", href: "/BoardGameTracker/android-chrome-192x192.png" } },
				{ tag: "link", attrs: { rel: "icon", type: "image/png", sizes: "512x512", href: "/BoardGameTracker/android-chrome-512x512.png" } },
				{ tag: "link", attrs: { rel: "apple-touch-icon", sizes: "180x180", href: "/BoardGameTracker/apple-touch-icon.png" } },
			],
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
					label: "Install",
					items: [
						"getting-started/quick-start",
						"getting-started/docker",
						"getting-started/upgrading",
						"getting-started/proxy",
					],
				},
				{
					label: "Configure",
					items: [
						"getting-started/environment-variables",
						"getting-started/authentication",
						"getting-started/email",
						"getting-started/rag",
						"extra/logging",
					],
				},
				{
					label: "User guide",
					items: ["user-guide"],
				},
				{
					label: "Contribute",
					items: ["extra/development", "extra/translations", "extra/bugs-features"],
				},
			],
		}),
	],
});
