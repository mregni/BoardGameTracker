import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

export const CtaSection = () => {
	const { t } = useTranslation();

	return (
		<section className="px-6 py-20">
			<div className="max-w-4xl mx-auto text-center">
				<h2 className="text-4xl text-white mb-6">
					{t("home.cta.title")}
				</h2>
				<p className="text-xl text-slate-300 mb-8">
					{t("home.cta.subtitle")}
				</p>
				<div className="flex flex-col sm:flex-row gap-4 justify-center">
					<Link
						to="/documentation"
						className="px-8 py-4 bg-purple-600 hover:bg-purple-500 text-white rounded-lg font-semibold transition-colors shadow-lg"
					>
						{t("home.cta.getStarted")}
					</Link>
					<Link
						to="/documentation"
						className="px-8 py-4 bg-white/10 hover:bg-white/20 text-white rounded-lg font-semibold transition-colors border border-white/20"
					>
						{t("home.cta.viewDocs")}
					</Link>
				</div>
			</div>
		</section>
	);
};
