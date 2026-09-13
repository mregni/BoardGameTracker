import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BgtText } from "@/components/BgtText/BgtText";
import { type ApiError, QUERY_KEYS } from "@/models";
import { getWatchInfoCall } from "@/services/changeDetectionService";

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface Props {
	watchId: string | undefined;
}

export const WatchIdPreview = ({ watchId }: Props) => {
	const { t } = useTranslation("game");
	const trimmed = (watchId ?? "").trim();
	const valid = GUID.test(trimmed);

	const query = useQuery({
		queryKey: [QUERY_KEYS.watchInfo, trimmed],
		queryFn: () => getWatchInfoCall(trimmed),
		enabled: valid,
		retry: false,
		staleTime: 5 * 60 * 1000,
	});

	if (!valid || query.isPending) {
		return null;
	}

	if (query.isError) {
		const status = (query.error as ApiError).status;
		return (
			<BgtText size="1" className="text-red-400">
				{t(status === 404 ? "watch-id.not-found" : "watch-id.unverified")}
			</BgtText>
		);
	}

	return (
		<BgtText size="1" className="text-white/50">
			{t("watch-id.tracking", { title: query.data.title ?? query.data.url })}
		</BgtText>
	);
};
