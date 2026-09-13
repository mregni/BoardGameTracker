import { useQuery } from "@tanstack/react-query";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { z } from "zod";
import ChatIcon from "@/assets/icons/chat.svg?react";
import List from "@/assets/icons/list.svg?react";
import { BgtSimpleSelect } from "@/components/BgtForm";
import { BgtEmptyState } from "@/components/BgtLayout/BgtEmptyState";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { getGames } from "@/services/queries/games";
import { getGameManuals } from "@/services/queries/manuals";
import { getSettings } from "@/services/queries/settings";
import { ChatComposer } from "./-components/ChatComposer";
import { ChatTranscript } from "./-components/ChatTranscript";
import { SourcesOverlay } from "./-components/SourcesOverlay";
import { SourcesPanel } from "./-components/SourcesPanel";
import { useRagChat } from "./-hooks/useRagChat";

interface FocusedSource {
	id: string;
	index: number;
}

const ALL_MANUALS = "all";

const chatSearchSchema = z.object({
	gameId: z.number().int().positive().optional().catch(undefined),
	manualId: z.number().int().positive().optional().catch(undefined),
});

export const Route = createFileRoute("/chat/")({
	component: RouteComponent,
	validateSearch: chatSearchSchema,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.ensureQueryData(getSettings());
		if (!settings.ragEnabled) {
			throw redirect({ to: "/" });
		}
	},
	loader: ({ context: { queryClient } }) => {
		queryClient.prefetchQuery(getGames());
	},
});

function RouteComponent() {
	const { t } = useTranslation(["chat", "common"]);
	const navigate = useNavigate();
	const search = Route.useSearch();
	const { ask, retry, getExchanges, isPending } = useRagChat();

	const { data: games = [] } = useQuery(getGames());
	const selectedGameId =
		search.gameId !== undefined && games.some((game) => game.id === search.gameId) ? search.gameId : undefined;

	const { data: manuals = [] } = useQuery({
		...getGameManuals(selectedGameId ?? 0),
		enabled: selectedGameId !== undefined,
	});

	const selectedManualId =
		selectedGameId !== undefined &&
		search.manualId !== undefined &&
		manuals.some((manual) => manual.id === search.manualId)
			? search.manualId
			: undefined;

	const gameItems = useMemo(
		() => games.map((game) => ({ value: game.id, label: game.title, image: game.image })),
		[games],
	);
	const manualItems = useMemo(
		() => [
			{ value: ALL_MANUALS, label: t("all-manuals") },
			...manuals.map((manual) => ({ value: manual.id, label: manual.title })),
		],
		[manuals, t],
	);

	const onGameChange = (value: string | number) => {
		navigate({ to: "/chat", search: { gameId: Number(value) }, replace: true });
	};

	const onManualChange = (value: string | number) => {
		if (value === ALL_MANUALS) {
			navigate({ to: "/chat", search: { gameId: selectedGameId }, replace: true });
			return;
		}
		navigate({ to: "/chat", search: { gameId: selectedGameId, manualId: Number(value) }, replace: true });
	};

	const indexedCount = manuals.filter((manual) => manual.indexStatus === "indexed").length;
	const canAsk = selectedGameId !== undefined && indexedCount > 0;
	const exchanges = getExchanges(selectedGameId);

	const [focused, setFocused] = useState<FocusedSource | null>(null);
	const [overlayOpen, setOverlayOpen] = useState(false);
	const lastAnsweredId = useRef<string | null>(null);
	const isDesktop = useMediaQuery("(min-width: 1024px)");

	// The sources panel follows the newest answer: it opens when that answer has
	// citations and collapses (chat goes full width) when the latest answer has none.
	useEffect(() => {
		const latest = [...exchanges].reverse().find((exchange) => exchange.status === "done");
		if (latest && latest.id !== lastAnsweredId.current) {
			lastAnsweredId.current = latest.id;
			setFocused((latest.answer?.citations.length ?? 0) > 0 ? { id: latest.id, index: 0 } : null);
		}
	}, [exchanges]);

	// Clear the panel/overlay when switching games.
	// biome-ignore lint/correctness/useExhaustiveDependencies: reset only when the selected game changes
	useEffect(() => {
		setFocused(null);
		setOverlayOpen(false);
		lastAnsweredId.current = null;
	}, [selectedGameId]);

	const focusedCitations = focused
		? (exchanges.find((exchange) => exchange.id === focused.id)?.answer?.citations ?? [])
		: [];
	const showPanel = focused !== null && focusedCitations.length > 0;

	const handleSelectSource = (exchangeId: string, index: number) => {
		setFocused({ id: exchangeId, index });
		// On desktop a chip just moves the docked panel; on mobile it opens the full-screen viewer.
		if (!isDesktop) {
			setOverlayOpen(true);
		}
	};

	const focusIndex = (index: number) => setFocused((current) => (current ? { ...current, index } : current));

	const handleSend = (question: string) => {
		if (selectedGameId === undefined) {
			return;
		}
		ask(selectedGameId, question, selectedManualId);
	};

	const renderBody = () => {
		if (selectedGameId === undefined) {
			return (
				<BgtEmptyState icon={ChatIcon} title={t("empty.no-game.title")} description={t("empty.no-game.description")} />
			);
		}
		if (manuals.length === 0) {
			return (
				<BgtEmptyState
					icon={List}
					title={t("empty.no-manuals.title")}
					description={t("empty.no-manuals.description")}
				/>
			);
		}
		if (indexedCount === 0) {
			return (
				<BgtEmptyState
					icon={List}
					title={t("empty.not-indexed.title")}
					description={t("empty.not-indexed.description")}
				/>
			);
		}
		return (
			<ChatTranscript
				exchanges={exchanges}
				isPending={isPending}
				emptyHint={t("empty.ask-something")}
				onRetry={(exchange) => retry(selectedGameId, exchange)}
				onSelectSource={handleSelectSource}
				focused={focused}
			/>
		);
	};

	return (
		<div className="flex min-h-[calc(100dvh-5rem)] flex-col gap-3 p-3 md:h-full md:min-h-0 md:overflow-hidden xl:px-6">
			<div className="flex shrink-0 flex-col gap-3">
				<BgtPageHeader header={t("title")} icon={ChatIcon} />
				<div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
					<BgtSimpleSelect
						items={gameItems}
						hasSearch
						placeholder={t("select-game")}
						value={selectedGameId ?? null}
						onValueChange={onGameChange}
						className="w-full sm:w-64"
					/>
					{selectedGameId !== undefined && manuals.length > 0 && (
						<BgtSimpleSelect
							items={manualItems}
							placeholder={t("all-manuals")}
							value={selectedManualId ?? ALL_MANUALS}
							onValueChange={onManualChange}
							className="w-full sm:w-64"
						/>
					)}
				</div>
			</div>

			<div className="flex min-h-0 flex-1 gap-4">
				<div className={`flex min-w-0 flex-1 flex-col ${showPanel ? "lg:max-w-3xl" : ""}`}>
					<div className="min-h-0 flex-1 overflow-y-auto">{renderBody()}</div>

					<footer className="sticky bottom-20 z-10 shrink-0 bg-background md:static">
						<ChatComposer
							disabled={!canAsk}
							pending={isPending}
							placeholder={canAsk ? t("composer.placeholder") : t("composer.disabled-placeholder")}
							onSend={handleSend}
						/>
					</footer>
				</div>

				{showPanel && (
					<aside className="hidden min-w-0 overflow-hidden rounded-xl border border-white/10 bg-card/40 p-4 motion-safe:animate-slide-in-right lg:flex lg:min-w-[420px] lg:flex-1">
						<SourcesPanel
							citations={focusedCitations}
							focusedIndex={focused?.index ?? 0}
							onFocus={focusIndex}
							onExpand={() => setOverlayOpen(true)}
						/>
					</aside>
				)}
			</div>

			{overlayOpen && focusedCitations.length > 0 && (
				<SourcesOverlay
					citations={focusedCitations}
					index={focused?.index ?? 0}
					onIndexChange={focusIndex}
					onClose={() => setOverlayOpen(false)}
				/>
			)}
		</div>
	);
}
