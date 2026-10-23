import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtDialog, BgtDialogContent, BgtDialogFooter, BgtDialogTitle } from "@/components/BgtDialog";
import { BgtSelect } from "@/components/BgtForm/BgtSelect";
import { useAuth } from "@/hooks/useAuth";
import { QUERY_KEYS } from "@/models";
import type { PlayerLinkMatchReason, PlayerLinkSuggestions } from "@/models/Auth/Auth";
import { useToasts } from "@/routes/-hooks/useToasts";
import { createOwnPlayerCall, linkOwnPlayerCall, skipPlayerLinkCall } from "@/services/authService";
import { getPlayerLinkSuggestions } from "@/services/queries/auth";
import { apiErrorMessage } from "@/utils/errorUtils";

const REASON_KEYS: Record<PlayerLinkMatchReason, string> = {
	email: "player-link.reason.email",
	name: "player-link.reason.name",
	partial: "player-link.reason.partial",
};

export const PlayerLinkPrompt = () => {
	const needsPlayerLink = useAuth((s) => s.user?.needsPlayerLink === true);
	return needsPlayerLink ? <PlayerLinkDialog /> : null;
};

const hasNothingToOffer = (data: PlayerLinkSuggestions) => data.players.length === 0 && !data.canCreatePlayer;

const PlayerLinkDialog = () => {
	const { t } = useTranslation("auth");
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();
	const updateUser = useAuth((s) => s.updateUser);
	const { data, refetch } = useQuery(getPlayerLinkSuggestions());
	const [selectedId, setSelectedId] = useState<number | null>(null);
	const autoSkipped = useRef(false);

	const close = () => updateUser({ needsPlayerLink: false });

	const onLinked = (message: string, name?: string) => {
		close();
		successToast(message, undefined, { name });
		for (const key of [
			QUERY_KEYS.players,
			QUERY_KEYS.player,
			QUERY_KEYS.profile,
			QUERY_KEYS.dashboard,
			QUERY_KEYS.users,
		]) {
			queryClient.invalidateQueries({ queryKey: [key] });
		}
	};

	const onFailed = (error: unknown) => {
		errorToast(apiErrorMessage(error, "auth:player-link.failed"));
		refetch();
	};

	const linkMutation = useMutation({
		mutationFn: (playerId: number) => linkOwnPlayerCall(playerId),
		onSuccess: (_profile, playerId) =>
			onLinked("auth:player-link.linked", data?.players.find((p) => p.id === playerId)?.name),
		onError: onFailed,
	});

	const createMutation = useMutation({
		mutationFn: createOwnPlayerCall,
		onSuccess: () => onLinked("auth:player-link.created"),
		onError: onFailed,
	});

	const skipMutation = useMutation({
		mutationFn: skipPlayerLinkCall,
		onSettled: close,
	});

	const nothingToOffer = data !== undefined && hasNothingToOffer(data);
	const { mutate: skip } = skipMutation;

	useEffect(() => {
		if (nothingToOffer && !autoSkipped.current) {
			autoSkipped.current = true;
			skip();
		}
	}, [nothingToOffer, skip]);

	if (data === undefined || nothingToOffer) {
		return null;
	}

	const hasPlayers = data.players.length > 0;
	const suggestedIds = new Set(data.suggestions.map((s) => s.id));
	const others = data.players.filter((p) => !suggestedIds.has(p.id));
	const chosenId = selectedId ?? data.suggestions[0]?.id ?? null;
	const busy = linkMutation.isPending || createMutation.isPending || skipMutation.isPending;

	const link = () => {
		if (chosenId !== null) {
			linkMutation.mutate(chosenId);
		}
	};

	const create = () => createMutation.mutate();

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent aria-describedby={undefined}>
				<BgtDialogTitle>{t(hasPlayers ? "player-link.title" : "player-link.title-new")}</BgtDialogTitle>
				{data.suggestions.length > 0 && (
					<fieldset className="flex flex-col gap-2">
						<legend className="sr-only">{t("player-link.title")}</legend>
						{data.suggestions.map((suggestion) => {
							const checked = chosenId === suggestion.id;
							return (
								<label
									key={suggestion.id}
									className={cx(
										"flex items-center gap-3 rounded-lg border p-3 transition-colors cursor-pointer has-focus-visible:outline-2 has-focus-visible:outline-primary",
										checked ? "border-primary bg-primary/20" : "border-white/10 hover:bg-white/5",
									)}
								>
									<input
										type="radio"
										name="player-link-suggestion"
										className="sr-only"
										checked={checked}
										onChange={() => setSelectedId(suggestion.id)}
									/>
									<BgtAvatar image={suggestion.image} title={suggestion.name} size="large" />
									<span className="flex min-w-0 flex-col">
										<span className="truncate font-semibold">{suggestion.name}</span>
										<span className="text-xs text-white/60">{t(REASON_KEYS[suggestion.reason])}</span>
									</span>
								</label>
							);
						})}
					</fieldset>
				)}
				{others.length > 0 && (
					<BgtSelect
						label={t(data.suggestions.length > 0 ? "player-link.someone-else" : "player-link.pick-player")}
						items={others.map((p) => ({ value: p.id, label: p.name, image: p.image }))}
						value={others.some((p) => p.id === chosenId) ? chosenId : null}
						onValueChange={(value) => setSelectedId(Number(value))}
						placeholder={t("player-link.pick-player")}
						hasSearch
						showAvatars
					/>
				)}
				<BgtDialogFooter
					onCancel={() => skip()}
					cancelLabel={t("player-link.skip")}
					secondaryLabel={hasPlayers && data.canCreatePlayer ? t("player-link.create") : undefined}
					onSecondary={create}
					confirmLabel={t(hasPlayers ? "player-link.confirm" : "player-link.create")}
					onConfirm={hasPlayers ? link : create}
					disabled={hasPlayers && chosenId === null}
					loading={busy}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
