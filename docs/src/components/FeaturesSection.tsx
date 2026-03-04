import { BarChart2, Calendar, Handshake, MapPin, Package, Shield } from "lucide-react";
import { useTranslation } from "react-i18next";
import { FeatureCard } from "./FeatureCard";

export const FeaturesSection = () => {
	const { t } = useTranslation();

	return (
		<section className="px-6 py-20 bg-slate-800/30">
			<div className="max-w-7xl mx-auto">
				<div className="text-center mb-12">
					<h2 className="text-4xl text-white mb-4">
						{t("home.features.title")}
					</h2>
					<p className="text-xl text-slate-300 max-w-2xl mx-auto">
						{t("home.features.subtitle")}
					</p>
				</div>

				<div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
					<FeatureCard
						icon={Calendar}
						title={t("home.features.gameNight.title")}
						description={t("home.features.gameNight.description")}
					/>
					<FeatureCard
						icon={Package}
						title={t("home.features.shelfOfShame.title")}
						description={t("home.features.shelfOfShame.description")}
					/>
					<FeatureCard
						icon={MapPin}
						title={t("home.features.locations.title")}
						description={t("home.features.locations.description")}
					/>
					<FeatureCard
						icon={Shield}
						title={t("home.features.oidc.title")}
						description={t("home.features.oidc.description")}
					/>
					<FeatureCard
						icon={Handshake}
						title={t("home.features.loans.title")}
						description={t("home.features.loans.description")}
					/>
					<FeatureCard
						icon={BarChart2}
						title={t("home.features.comparison.title")}
						description={t("home.features.comparison.description")}
					/>
				</div>
			</div>
		</section>
	);
};
