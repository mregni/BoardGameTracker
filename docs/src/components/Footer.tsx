import { useTranslation } from "react-i18next";

export const Footer = () => {
	const { t } = useTranslation();

	return (
		<footer className="px-6 py-12 border-t border-white/10">
			<div className="max-w-7xl mx-auto text-center text-slate-400">
				<p>{t("footer.copyright")}</p>
			</div>
		</footer>
	);
};
