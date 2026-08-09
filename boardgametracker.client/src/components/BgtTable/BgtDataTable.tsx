import {
	type ColumnDef,
	flexRender,
	getCoreRowModel,
	getFilteredRowModel,
	getSortedRowModel,
	type RowData,
	useReactTable,
} from "@tanstack/react-table";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";

import { BgtTable, BgtTableBody, BgtTableCell, BgtTableHead, BgtTableHeader, BgtTableRow } from "./BgtTable";

declare module "@tanstack/react-table" {
	interface ColumnMeta<TData extends RowData, TValue> {
		hideOnMobile?: boolean;
	}
}

export interface DataTableProps<T> {
	columns: ColumnDef<T>[];
	data: T[];
	noDataMessage: string;
	size?: "sm" | "md";
	isLoading?: boolean;
	noHeaders?: boolean;
	firstCellClassNames?: string;
	widths?: (string | null)[];
}

export const BgtDataTable = <T,>(props: DataTableProps<T>) => {
	const { columns, data, size = "md", noDataMessage, isLoading = false, widths, noHeaders } = props;
	const { t } = useTranslation();
	const table = useReactTable({
		data,
		columns,
		getCoreRowModel: getCoreRowModel(),
		getSortedRowModel: getSortedRowModel(),
		getFilteredRowModel: getFilteredRowModel(),
	});

	const headers = table.getHeaderGroups().map((header) => (
		<BgtTableRow key={header.id}>
			{header.headers.map((head, i) => {
				const canSort = head.column.getCanSort();
				const sorted = head.column.getIsSorted();
				const content = flexRender(head.column.columnDef.header, head.getContext());
				const hideOnMobile = head.column.columnDef.meta?.hideOnMobile;
				return (
					<BgtTableHead
						scope="col"
						key={head.id}
						className={cx(
							widths?.[i] ?? "",
							i === header.headers.length - 1 && "text-right",
							hideOnMobile && "hidden md:table-cell",
						)}
					>
						{canSort ? (
							<button
								type="button"
								onClick={head.column.getToggleSortingHandler()}
								className="inline-flex items-center gap-1 cursor-pointer hover:text-primary"
							>
								{content}
								{sorted === "asc" && <CaretUpIcon className="size-3" />}
								{sorted === "desc" && <CaretDownIcon className="size-3" />}
							</button>
						) : (
							content
						)}
					</BgtTableHead>
				);
			})}
		</BgtTableRow>
	));

	const cellClasses = cx("whitespace-nowrap", {
		"px-2.5 py-1 text-xs": size === "sm",
		"px-3 py-3 text-sm": size === "md",
	});

	let rows: React.ReactNode;

	if (isLoading) {
		rows = (
			<BgtTableRow>
				<BgtTableCell colSpan={columns.length} className="h-10">
					{t("loading-data")}
				</BgtTableCell>
			</BgtTableRow>
		);
	}

	if (!isLoading && table.getRowModel().rows?.length) {
		rows = table.getRowModel().rows.map((row) => (
			<BgtTableRow key={row.id} data-state={row.getIsSelected() && "selected"}>
				{row.getVisibleCells().map((cell, i) => {
					const hideOnMobile = cell.column.columnDef.meta?.hideOnMobile;
					return (
						<BgtTableCell
							key={cell.id}
							className={cx(
								i === row.getVisibleCells().length - 1 && "text-right",
								hideOnMobile && "hidden md:table-cell",
								cellClasses,
							)}
						>
							{flexRender(cell.column.columnDef.cell, cell.getContext())}
						</BgtTableCell>
					);
				})}
			</BgtTableRow>
		));
	}

	if (!isLoading && table.getRowModel().rows?.length === 0) {
		rows = (
			<BgtTableRow>
				<BgtTableCell colSpan={columns.length} className="items-center text-center h-9 pt-4">
					{noDataMessage}
				</BgtTableCell>
			</BgtTableRow>
		);
	}

	return (
		<BgtTable>
			{!noHeaders && <BgtTableHeader>{headers}</BgtTableHeader>}
			<BgtTableBody>{rows}</BgtTableBody>
		</BgtTable>
	);
};
