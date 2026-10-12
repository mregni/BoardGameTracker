import { useTranslation } from "react-i18next";

interface Props {
	count: number;
}

export const BgtTileSessions = (props: Props) => {
	const { count } = props;
	const { t } = useTranslation("common");

	if (count === 0) {
		return <>{t("tile.no-sessions")}</>;
	}

	return (
		<>
			<span className="font-semibold text-white">{count}</span> {t("tile.sessions", { count })}
		</>
	);
};
