import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { cx } from "class-variance-authority";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import PlusIcon from "@/assets/icons/plus.svg?react";
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

const NEW_PLAYER = "new";
type Choice = number | typeof NEW_PLAYER;

export const PlayerLinkPrompt = () => {
	const needsPlayerLink = useAuth((s) => s.user?.needsPlayerLink === true);
	return needsPlayerLink ? <PlayerLinkDialog /> : null;
};

const hasNothingToOffer = (data: PlayerLinkSuggestions) => data.players.length === 0 && !data.canCreatePlayer;

const titleKey = (data: PlayerLinkSuggestions) => {
	if (data.players.length === 0) return "player-link.title-new";
	return data.suggestions.length === 1 ? "player-link.title-one" : "player-link.title";
};

interface ChoiceCardProps {
	checked: boolean;
	disabled: boolean;
	onSelect: () => void;
	icon: ReactNode;
	title: string;
	hint: string;
}

const ChoiceCard = (props: ChoiceCardProps) => {
	const { checked, disabled, onSelect, icon, title, hint } = props;
	return (
		<label
			className={cx(
				"flex items-center gap-3 rounded-lg border p-3 transition-colors cursor-pointer has-focus-visible:outline-2 has-focus-visible:outline-primary has-disabled:cursor-default has-disabled:opacity-60",
				checked ? "border-primary bg-primary/20" : "border-white/10 hover:bg-white/5",
			)}
		>
			<input
				type="radio"
				name="player-link-choice"
				className="sr-only"
				checked={checked}
				disabled={disabled}
				onChange={onSelect}
			/>
			{icon}
			<span className="flex min-w-0 flex-col">
				<span className="truncate font-semibold">{title}</span>
				<span className="text-xs text-white/60">{hint}</span>
			</span>
		</label>
	);
};

const PlayerLinkDialog = () => {
	const { t } = useTranslation("auth");
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();
	const updateUser = useAuth((s) => s.updateUser);
	const { data, refetch } = useQuery(getPlayerLinkSuggestions());
	const [selected, setSelected] = useState<Choice | null>(null);
	const autoSkipped = useRef(false);
	const choicesRef = useRef<HTMLFieldSetElement>(null);

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
	const offerNew = hasPlayers && data.canCreatePlayer;
	const suggestedIds = new Set(data.suggestions.map((s) => s.id));
	const others = data.players.filter((p) => !suggestedIds.has(p.id));
	const chosen: Choice | null = hasPlayers ? (selected ?? data.suggestions[0]?.id ?? null) : NEW_PLAYER;
	const busy = linkMutation.isPending || createMutation.isPending || skipMutation.isPending;

	const confirm = () => {
		if (chosen === NEW_PLAYER) {
			createMutation.mutate();
		} else if (chosen !== null) {
			linkMutation.mutate(chosen);
		}
	};

	const dismiss = () => {
		if (!busy) {
			skip();
		}
	};

	const focusChoice = (event: Event) => {
		const root = choicesRef.current;
		const target =
			root?.querySelector<HTMLInputElement>("input:checked") ?? root?.querySelector<HTMLInputElement>("input");
		if (target) {
			event.preventDefault();
			target.focus();
		}
	};

	return (
		<BgtDialog open onClose={dismiss}>
			<BgtDialogContent aria-describedby={undefined} onOpenAutoFocus={focusChoice}>
				<BgtDialogTitle>{t(titleKey(data))}</BgtDialogTitle>
				{(data.suggestions.length > 0 || offerNew) && (
					<fieldset ref={choicesRef} className="flex flex-col gap-2">
						<legend className="sr-only">{t(titleKey(data))}</legend>
						{data.suggestions.map((suggestion) => (
							<ChoiceCard
								key={suggestion.id}
								checked={chosen === suggestion.id}
								disabled={busy}
								onSelect={() => setSelected(suggestion.id)}
								icon={<BgtAvatar image={suggestion.image} title={suggestion.name} size="large" />}
								title={suggestion.name}
								hint={t(REASON_KEYS[suggestion.reason])}
							/>
						))}
						{offerNew && (
							<ChoiceCard
								checked={chosen === NEW_PLAYER}
								disabled={busy}
								onSelect={() => setSelected(NEW_PLAYER)}
								icon={
									<span className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-white/10">
										<PlusIcon className="size-5" aria-hidden />
									</span>
								}
								title={t("player-link.new.title")}
								hint={t("player-link.new.hint")}
							/>
						)}
					</fieldset>
				)}
				{others.length > 0 && (
					<BgtSelect
						label={t(data.suggestions.length > 0 ? "player-link.someone-else" : "player-link.pick-player")}
						items={others.map((p) => ({ value: p.id, label: p.name, image: p.image }))}
						value={typeof chosen === "number" && others.some((p) => p.id === chosen) ? chosen : null}
						onValueChange={(value) => setSelected(Number(value))}
						placeholder={t("player-link.search-players")}
						disabled={busy}
						hasSearch
						showAvatars
					/>
				)}
				<p className="text-xs text-white/60">{t("player-link.later-hint")}</p>
				<BgtDialogFooter
					onCancel={dismiss}
					cancelLabel={t("player-link.skip")}
					confirmLabel={t(chosen === NEW_PLAYER ? "player-link.create" : "player-link.confirm")}
					onConfirm={confirm}
					disabled={chosen === null}
					loading={busy}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
