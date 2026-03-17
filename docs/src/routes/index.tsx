import { createFileRoute } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { CtaSection } from "../components/CtaSection";
import { FeaturesSection } from "../components/FeaturesSection";
import { Section } from "../components/Section";

export const Route = createFileRoute("/")({
	component: RouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation();

	return (
		<>
			<section className="px-6 py-10">
				<div className="max-w-7xl mx-auto">
					<div className="text-center mb-6">
						<h1 className="text-5xl md:text-6xl text-white mb-2">
							{t("home:title")}
						</h1>
						<p className="text-xl text-slate-300 max-w-3xl mx-auto">
							{t("home:subtitle")}
						</p>
					</div>

					<div className="rounded-xl overflow-hidden flex justify-center">
						<img
							src={`${import.meta.env.BASE_URL}images/hero.png`}
							alt={t("home:heroAlt")}
							className="w-[80%] h-auto"
						/>
					</div>
					<p className="text-center text-purple-300/70 italic mt-2 text-lg">
						I created this because I needed proof that my wife always wins
					</p>
				</div>
			</section>

			<Section
				title={t("home:dashboard.title")}
				description={t("home:dashboard.description")}
				bullets={
					t("home:dashboard.bullets", { returnObjects: true }) as string[]
				}
				imageSrc={`${import.meta.env.BASE_URL}images/dashboard.png`}
				imageAlt={t("home:dashboard.imageAlt")}
				background
			/>

			<Section
				title={t("home:games.title")}
				description={t("home:games.description")}
				bullets={t("home:games.bullets", { returnObjects: true }) as string[]}
				imageSrc={`${import.meta.env.BASE_URL}images/games.png`}
				imageAlt={t("home:games.imageAlt")}
				reverse
			/>

			<Section
				title={t("home:sessions.title")}
				description={t("home:sessions.description")}
				bullets={
					t("home:sessions.bullets", { returnObjects: true }) as string[]
				}
				imageSrc={`${import.meta.env.BASE_URL}images/session.png`}
				imageAlt={t("home:sessions.imageAlt")}
				background
			/>

			<Section
				title={t("home:players.title")}
				description={t("home:players.description")}
				bullets={t("home:players.bullets", { returnObjects: true }) as string[]}
				imageSrc={`${import.meta.env.BASE_URL}images/player.png`}
				imageAlt={t("home:players.imageAlt")}
				reverse
			/>

			<FeaturesSection />

			<CtaSection />
		</>
	);
}
