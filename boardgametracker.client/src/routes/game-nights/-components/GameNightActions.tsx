import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import type { GameNight } from "@/models";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";
import { sendInvitesCall } from "@/services/gameNightService";
import { apiErrorMessage } from "@/utils/errorUtils";
import { useInviteLink } from "../-hooks/useInviteLink";

interface Props {
	gameNight: GameNight;
	onEdit: (gameNight: GameNight) => void;
	onDelete: (gameNight: GameNight) => void;
}

export const GameNightActions = (props: Props) => {
	const { gameNight, onEdit, onDelete } = props;
	const { t } = useTranslation("game-nights");
	const { copied, copy } = useInviteLink(gameNight);
	const { settings } = useSettingsData();

	const sendInvitesMutation = useMutation({
		mutationFn: () => sendInvitesCall(gameNight.id),
		onSuccess: (result) => {
			toast.success(t("card.invites-sent", { sent: result.sent }));
		},
		onError: (error) => {
			toast.error(apiErrorMessage(error, "game-nights:card.invites-failed"));
		},
	});

	return (
		<div className="flex flex-wrap items-center gap-2 md:justify-end">
			<BgtButton variant="cancel" size="1" onClick={copy} disabled={copied}>
				{copied ? t("card.copied") : t("card.copy-link")}
			</BgtButton>
			{settings?.emailEnabled && (
				<BgtButton
					variant="cancel"
					size="1"
					onClick={() => sendInvitesMutation.mutate()}
					disabled={sendInvitesMutation.isPending}
				>
					{t("card.email-invites")}
				</BgtButton>
			)}
			<BgtEditDeleteButtons onEdit={() => onEdit(gameNight)} onDelete={() => onDelete(gameNight)} />
		</div>
	);
};
