import { useCallback, useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import type { GameNight } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { effectivePublicUrl } from "@/utils/publicUrl";

export const useInviteLink = (gameNight: Pick<GameNight, "linkId">) => {
	const { t } = useTranslation("game-nights");
	const { settings } = useSettingsData();
	const [copied, setCopied] = useState(false);
	const timeout = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

	const publicUrl = effectivePublicUrl(settings?.publicUrl, window.location.origin);
	const link = `${publicUrl}/rsvp?linkId=${gameNight.linkId}`;

	useEffect(() => () => clearTimeout(timeout.current), []);

	const copy = useCallback(() => {
		const write = async () => {
			await navigator.clipboard.writeText(link);
			setCopied(true);
			clearTimeout(timeout.current);
			timeout.current = setTimeout(() => setCopied(false), 2000);
		};
		write().catch(() => toast.error(t("card.copy-failed")));
	}, [link, t]);

	return { link, copied, copy, emailEnabled: !!settings?.emailEnabled };
};
