import { useTranslation } from "react-i18next";
import Thumb from "@/assets/icons/thumb.svg?react";
import { BgtCard } from "@/components/BgtCard/BgtCard";

export const NoShames = () => {
	const { t } = useTranslation("shames");

	return (
		<BgtCard>
			<div className="p-8 text-center">
				<Thumb className="text-green-500 mx-auto mb-4" />
				<h3 className="text-xl text-white mb-2">{t("none.title")}</h3>
				<p className="text-gray-400">{t("none.description")}</p>
			</div>
		</BgtCard>
	);
};
