import { useCallback, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtText } from "@/components/BgtText/BgtText";
import type { GameNight } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";

interface Props {
	gameNight: GameNight;
	onManageRsvps: (gameNight: GameNight) => void;
}

export const GameNightActions = (props: Props) => {
	const { gameNight, onManageRsvps } = props;
	const { t } = useTranslation();
	const { settings } = useSettingsData({});

	const publicUrl = settings?.publicUrl || window.location.origin;
	const rsvpLink = `${publicUrl}?rsvp=${gameNight.linkId}`;

	const [copied, setCopied] = useState(false);

	const onCopyLink = useCallback(() => {
		navigator.clipboard.writeText(rsvpLink);
		setCopied(true);
		setTimeout(() => setCopied(false), 2000);
	}, [rsvpLink]);

	return (
		<div className="bg-primary/10 border border-primary/30 rounded-lg p-3">
			<div className="flex items-center justify-between gap-4 flex-wrap">
				<div className="flex items-center gap-2 flex-1 min-w-0">
					<div className="flex-1 min-w-0">
						<BgtText size="2" weight="medium" color="gray" className="mb-1">
							{t("game-nights.card.rsvp-link")}
						</BgtText>
						<BgtText size="1" color="gray" className="truncate">
							{rsvpLink}
						</BgtText>
					</div>
				</div>
				<div className="flex gap-2 shrink-0">
					<BgtButton variant="cancel" size="1" onClick={() => onManageRsvps(gameNight)}>
						{t("game-nights.card.manage-rsvps")}
					</BgtButton>
					<BgtButton variant="primary" size="1" onClick={onCopyLink} disabled={copied}>
						{copied ? t("game-nights.card.copied") : t("game-nights.card.copy-link")}
					</BgtButton>
				</div>
			</div>
		</div>
	);
};
