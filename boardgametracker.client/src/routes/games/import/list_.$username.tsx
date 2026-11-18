import { noop } from "@tanstack/react-query";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import Database from "@/assets/icons/database.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSimpleSwitch } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { BgtPaging } from "@/components/BgtTable/BgtPaging";
import { BgtText } from "@/components/BgtText/BgtText";
import type { ImportGame } from "@/models";
import { IntegrationSettingsButton } from "@/routes/-components/IntegrationSettingsButton";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { getBggCollection, getGames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import {
	AddedDateCell,
	AvatarCell,
	BggIdCell,
	CheckboxCell,
	ImportListContext,
	NameCell,
	PriceCell,
	ScoringCell,
	SelectAllHeader,
	StateCell,
} from "./-components/ImportListCells";
import { useList } from "./-hooks/useList";
import { needsIntegrationSettings } from "./-utils/importErrors";

export const Route = createFileRoute("/games/import/list_/$username")({
	component: WriteRouteComponent,
	beforeLoad: async ({ context: { queryClient } }) => {
		const settings = await queryClient.query({ ...getSettings(), staleTime: "static" });
		if (!settings.bggStatus?.isConfigured) {
			throw redirect({ to: "/games" });
		}
	},
	loader: ({ params, context: { queryClient } }) => {
		queryClient.query(getBggCollection(params.username)).catch(noop);
		queryClient.query(getGames()).catch(noop);
		queryClient.query(getSettings()).catch(noop);
	},
});

const countPerPage = 50;
const maxImport = 25;

function RouteComponent() {
	const { username } = Route.useParams();
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);

	const {
		bggError,
		bggErrorKind,
		retryCollection,
		settings,
		games,
		updateGame,
		setSelection,
		filterCollected,
		setFilterCollected,
		inCollectionCount,
		processingGames,
		totalCount,
		startImport,
		importing,
		importProgress,
	} = useList({
		username,
	});

	const [page, setPage] = useState<number>(0);

	const checkedCount = useMemo(() => games.filter((game) => game.checked).length, [games]);
	const selectableGames = useMemo(() => games.filter((game) => !game.inCollection), [games]);
	const selectionCap = Math.min(maxImport, selectableGames.length);
	const allSelected = selectionCap > 0 && checkedCount >= selectionCap;

	const toggleSelectAll = useCallback(
		(checked: boolean) => {
			setSelection(
				selectableGames.map((game, index) => ({ bggId: game.bggId, checked: checked && index < maxImport })),
			);
			if (checked) setPage(0);
		},
		[selectableGames, setSelection],
	);

	const importList = useMemo(
		() => ({
			selectAll: { checked: allSelected, disabled: selectionCap === 0, onChange: toggleSelectAll },
			updateGame,
			currency: settings?.currency,
		}),
		[allSelected, selectionCap, toggleSelectAll, updateGame, settings?.currency],
	);

	const columns: DataTableProps<ImportGame>["columns"] = useMemo(
		() => [
			{ accessorKey: "0", enableSorting: false, cell: CheckboxCell, header: SelectAllHeader },
			{ accessorKey: "1", cell: AvatarCell, header: "" },
			{ accessorKey: "2", cell: NameCell, header: t("name") },
			{ accessorKey: "3", cell: BggIdCell, header: t("common:bgg-id") },
			{ accessorKey: "4", cell: PriceCell, header: t("game:price.label") },
			{ accessorKey: "5", cell: StateCell, header: t("state") },
			{ accessorKey: "6", cell: ScoringCell, header: t("scoring") },
			{ accessorKey: "7", cell: AddedDateCell, header: t("game:added-date.label") },
		],
		[t],
	);

	const isLoading = processingGames || importing;

	const triggerImport = () => {
		startImport(games.filter((game) => game.checked));
	};

	const loadingLabel = importing ? t("games:import.importing", importProgress) : undefined;

	return (
		<BgtPage>
			<BgtPageHeader header={t("bgg-import:title")} actions={[]} icon={Database} />
			<BgtPageContent>
				{bggError && (
					<div className="flex flex-col gap-4">
						<BgtStatus
							variant="warning"
							title={t("games:import.error-title")}
							description={t(`games:import.error-${bggErrorKind}`)}
						/>
						<div className="flex flex-wrap gap-2">
							<BgtButton type="button" variant="primary" onClick={() => void retryCollection()}>
								{t("common:retry")}
							</BgtButton>
							{needsIntegrationSettings(bggErrorKind) && <IntegrationSettingsButton />}
						</div>
					</div>
				)}
				{!bggError && isLoading && <BgtLoadingSpinner label={loadingLabel} />}
				{!bggError && !isLoading && (
					<>
						<div className="flex flex-row justify-between gap-4 mb-16">
							<div className="flex flex-col gap-2 flex-1">
								<BgtText>
									{t("games:import.intro", {
										count: totalCount,
										collectionCount: inCollectionCount,
										maxImport,
									})}
								</BgtText>
								<BgtSimpleSwitch
									label={t("games:import.hide-collected")}
									value={filterCollected}
									onChange={(value) => setFilterCollected(value)}
								/>
							</div>
							<div>
								<BgtButton
									onClick={() => triggerImport()}
									disabled={checkedCount === 0 || checkedCount > maxImport}
									variant="primary"
								>
									{t("games:import.start-import", {
										count: checkedCount,
										totalCount: maxImport,
									})}
								</BgtButton>
							</div>
						</div>

						<BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />

						<ImportListContext.Provider value={importList}>
							<BgtDataTable
								columns={columns}
								data={games.slice(page * countPerPage, (page + 1) * countPerPage)}
								noDataMessage={t("no-data")}
								enableSorting={false}
								widths={["w-[48px]", "w-[70px]", null, "w-[100px]"]}
								rowClassName={(game) => (game.baseGameBggId != null ? "bg-card-black/60" : undefined)}
							/>
						</ImportListContext.Provider>

						<BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />
					</>
				)}
			</BgtPageContent>
		</BgtPage>
	);
}

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
