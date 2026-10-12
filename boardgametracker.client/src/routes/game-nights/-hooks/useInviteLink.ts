import { useState } from "react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { useTimeouts } from "@/hooks/useTimeouts";
import type { GameNight } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { effectivePublicUrl } from "@/utils/publicUrl";

export const useInviteLink = (gameNight: Pick<GameNight, "linkId">) => {
	const { t } = useTranslation("game-nights");
	const { settings } = useSettingsData();
	const [copied, setCopied] = useState(false);
	const later = useTimeouts();

	const link = `${effectivePublicUrl(settings?.publicUrl, window.location.origin)}/rsvp?linkId=${gameNight.linkId}`;

	const copy = () => {
		Promise.resolve()
			.then(() => navigator.clipboard.writeText(link))
			.then(() => {
				setCopied(true);
				later(() => setCopied(false), 2000);
			})
			.catch(() => toast.error(t("card.copy-failed")));
	};

	return { link, copied, copy };
};
