import { useTranslation } from "react-i18next";

interface PageTitleProps {
	translationKey: string;
}

export const PageTitle = ({ translationKey }: PageTitleProps) => {
	const { t } = useTranslation();

	return (
		<h1 className="text-4xl font-bold text-white mb-8">{t(translationKey)}</h1>
	);
};
