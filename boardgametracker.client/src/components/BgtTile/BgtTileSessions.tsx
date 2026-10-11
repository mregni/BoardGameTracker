import type { ReactNode } from "react";
import { useTranslation } from "react-i18next";

interface Props {
	count: number;
	prefix?: ReactNode;
}

export const BgtTileSessions = (props: Props) => {
	const { count, prefix } = props;
	const { t } = useTranslation("common");

	if (count === 0) {
		return <>{t("tile.no-sessions")}</>;
	}

	return (
		<>
			{prefix && <>{prefix} · </>}
			<span className="font-semibold text-white">{count}</span> {t("tile.sessions", { count })}
		</>
	);
};
