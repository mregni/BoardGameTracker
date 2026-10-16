import { useTranslation } from "react-i18next";

import { BgtText } from "@/components/BgtText/BgtText";

interface Props {
	variable: string | undefined;
}

export const EnvOverrideHint = ({ variable }: Props) => {
	const { t } = useTranslation("settings");

	if (!variable) {
		return null;
	}

	return (
		<BgtText size="1" color="amber">
			{t("env-override", { variable })}
		</BgtText>
	);
};
