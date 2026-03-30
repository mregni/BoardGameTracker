import { createFileRoute } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import LinkIcon from "@/assets/icons/arrow-square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtSimpleCheckbox, BgtSimpleInputField, BgtSimpleSelect, BgtSimpleSwitch } from "@/components/BgtForm";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtDataTable, type DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { BgtPaging } from "@/components/BgtTable/BgtPaging";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameState, type ImportGame } from "@/models";
import { getBggCollection, getGames } from "@/services/queries/games";
import { getSettings } from "@/services/queries/settings";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { ImportLoader } from "./-components/ImportLoader";
import { useList } from "./-hooks/useList";

export const Route = createFileRoute("/games/import/list_/$username")({
	component: RouteComponent,
	loader: async ({ params, context: { queryClient } }) => {
		queryClient.prefetchQuery(getBggCollection(params.username));
		queryClient.prefetchQuery(getGames());
		queryClient.prefetchQuery(getSettings());
	},
});

const countPerPage = 50;
const maxImport = 5;

function RouteComponent() {
	const { username } = Route.useParams();
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	const [loadText, setLoadText] = useState("games:import.loading");

	const onSuccess = () => {
		setLoadText("games:import.loading");
	};

	const {
		statusCode,
		settings,
		games,
		updateGame,
		filterCollected,
		setFilterCollected,
		inCollectionCount,
		processingGames,
		totalCount,
		startImport,
		importing,
	} = useList({
		username,
		onSuccess,
	});

	const [page, setPage] = useState<number>(0);

	const columns: DataTableProps<ImportGame>["columns"] = useMemo(
		() => [
			{
				accessorKey: "0",
				cell: ({ row }) => (
					<BgtSimpleCheckbox
						id={""}
						disabled={row.original.inCollection}
						label={""}
						checked={row.original.checked}
						onCheckedChange={(state) => {
							updateGame(row.original.bggId, { checked: state });
						}}
					/>
				),
				header: "",
			},
			{
				accessorKey: "1",
				cell: ({ row }) => (
					<BgtAvatar
						title={row.original.title}
						image={row.original.imageUrl}
						size="large"
						disabled={row.original.inCollection}
					/>
				),
				header: "",
			},
			{
				accessorKey: "2",
				cell: ({ row }) => <div className={cx(row.original.inCollection && "text-gray-500")}>{row.original.title}</div>,
				header: t("name"),
			},
			{
				accessorKey: "3",
				cell: ({ row }) => (
					<div>
						<a
							className="underline text-blue-700 cursor-pointer flex flex-row gap-1"
							href={`https://boardgamegeek.com/boardgame/${row.original.bggId}`}
							target="_blank"
							rel="noopener noreferrer"
						>
							{row.original.bggId} <LinkIcon className="size-4" />
						</a>
					</div>
				),
				header: t("name"),
			},
			{
				accessorKey: "4",
				cell: ({ row }) => (
					<BgtSimpleInputField
						type="number"
						value={row.original.price}
						onChange={(event) =>
							updateGame(row.original.bggId, {
								price: Number(event.target.value),
							})
						}
						placeholder={t("game:price.placeholder")}
						disabled={row.original.inCollection}
						className="w-[130px]"
						prefixLabel={settings?.currency}
					/>
				),
				header: t("game:price.label"),
			},
			{
				accessorKey: "5",
				cell: ({ row }) => (
					<BgtSimpleSelect
						value={row.original.state}
						onValueChange={(value) => updateGame(row.original.bggId, { state: value as GameState })}
						disabled={row.original.inCollection}
						items={Object.values(GameState).map((value) => ({
							label: t(getItemStateTranslationKey(value, false)),
							value: value,
						}))}
					/>
				),
				header: t("state"),
			},
			{
				accessorKey: "6",
				cell: ({ row }) => (
					<BgtSimpleSwitch
						label={t("game:scoring.label")}
						value={row.original.hasScoring}
						disabled={row.original.inCollection}
						onChange={(value) => updateGame(row.original.bggId, { hasScoring: value })}
					/>
				),
				header: t("scoring"),
			},
			{
				accessorKey: "7",
				cell: ({ row }) => (
					<BgtSimpleInputField
						value={row.original.addedDate}
						onChange={(event) =>
							updateGame(row.original.bggId, {
								addedDate: new Date(event.target.value),
							})
						}
						type="date"
						disabled={row.original.inCollection}
					/>
				),
				header: t("game:added-date.label"),
			},
		],
		[settings?.currency, t, updateGame],
	);

	const checkedCount = useMemo(() => {
		return games.filter((game) => game.checked).length;
	}, [games]);

	const triggerImport = () => {
		setLoadText("games:import.importing");
		startImport(games.filter((game) => game.checked));
	};

	return (
		<BgtPage>
			<BgtPageHeader header={t("bgg-import:title")} actions={[]} />
			<BgtPageContent>
				<ImportLoader show={() => statusCode !== 200 || processingGames || importing} text={t(loadText)}>
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
								label={"Hide games that are in your collection already"}
								value={filterCollected}
								onChange={(value) => setFilterCollected(value)}
							/>
						</div>
						<div>
							<BgtButton onClick={() => triggerImport()} disabled={checkedCount > maxImport} variant="primary">
								{t("games:import.start-import", {
									count: checkedCount,
									totalCount: maxImport,
								})}
							</BgtButton>
						</div>
					</div>

					<BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />

					<BgtDataTable
						columns={columns}
						data={games.slice(page * countPerPage, (page + 1) * countPerPage)}
						noDataMessage={t("no-data")}
						widths={["w-[48px]", "w-[70px]", null, "w-[100px]"]}
					/>

					<BgtPaging page={page} setPage={setPage} totalCount={games.length} countPerPage={countPerPage} />
				</ImportLoader>
			</BgtPageContent>
		</BgtPage>
	);
}
