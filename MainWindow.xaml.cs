﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;

namespace TestProjectAsyncUITimer {

    public partial class MainWindow : Window {

        private FilePrinter file_printer = new FilePrinter(Define.FILENAME);

        private BackgroundWorker worker;
        private SynchronizationContext sc = SynchronizationContext.Current;

        private Stopwatch stopwatch = new Stopwatch();

        private Thread thread = null;
        private bool abort_thread = false;

        public delegate void OnThreadFinishDelegate();

        public MainWindow() {

            //Factory Method
            InitializeComponent();

            InitializeWorker();
        }

        private void InitializeWorker() {

            worker = new BackgroundWorker();

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
 
            //worker.DoWork += worker_DoWork;
            //worker.ProgressChanged += worker_ProgressChanged;

            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
        }

        /* Background Worker *//* Background Worker *//* Background Worker *//* Background Worker *//* Background Worker *//* Background Worker *//* Background Worker */
        private void start_pb_Click(object sender, RoutedEventArgs e) {

            if (worker.IsBusy == false) {
                start_pb.IsEnabled = false;
                cancel_pb.IsEnabled = true;

                start_pb_2.IsEnabled = false;

                //Statistics
                stopwatch.Reset();
                stopwatch.Start();

                worker.RunWorkerAsync();
            }
        }

        private void cancel_pb_Click(object sender, RoutedEventArgs e) {

            if (worker.IsBusy){
                worker.CancelAsync();

                //Statistics
                stopwatch.Reset();
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e) {
       
            file_printer.create();

            for (int i = 0; i < Define.TIMES; i++) {

                if (i % (Define.TIMES/100) == 0)
                    (sender as BackgroundWorker).ReportProgress((int)((double)i / Define.TIMES * 100));

                file_printer.append(i+1);

                if (worker.CancellationPending) {

                    // Set the Cancel flag so that the WorkerCompleted event knows that the process was cancelled.
                    e.Cancel = true;
                    worker.ReportProgress(0);

                    //Accessing UI Thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        start_pb.IsEnabled = true;
                        cancel_pb.IsEnabled = false;

                        start_pb_2.IsEnabled = true;
                    });

                    return;
                }
            }
            worker.ReportProgress(100);
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            pb.Value = e.ProgressPercentage;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if (e.Cancelled) {
                pb_text.Foreground = Brushes.Red;
                pb_text.Text = "Job Cancelled...";

            } else if (e.Error != null) {
                pb_text.Foreground = Brushes.Red;
                pb_text.Text = "Error occured!";

            } else {
                pb_text.Foreground = Brushes.Green;
                pb_text.Text = "Job Completed!";

                MessageBox.Show(Define.PATH + "\\" + Define.FILENAME + "\n\n Watch ticks: " + stopwatch.ElapsedTicks, "Background Worker Message", MessageBoxButton.OK, MessageBoxImage.Information);

                openFile();
            }

            start_pb.IsEnabled = true;
            cancel_pb.IsEnabled = false;

            start_pb_2.IsEnabled = true;
        }

        /* Synchronization Context *//* Synchronization Context *//* Synchronization Context *//* Synchronization Context *//* Synchronization Context */
        private void start_pb_Click_2(object sender, RoutedEventArgs e) {

            OnThreadFinishDelegate finish_callback = new OnThreadFinishDelegate(onFinish);

            start_pb_2.IsEnabled = false;
            cancel_pb_2.IsEnabled = true;

            start_pb.IsEnabled = false;

            abort_thread = false;

            //Statistics
            stopwatch.Reset();
            stopwatch.Start();

            thread = new Thread(() =>
            {

                for (int i = 0; i < Define.TIMES; i++) {

                    if (abort_thread == true)
                        return;

                    if (i % (Define.TIMES / 100) == 0) {

                        sc.Post(new SendOrPostCallback(o =>
                        {
                            pb_2.Value = (int)((double)i / Define.TIMES * 100);

                        }), null);
                    }

                    file_printer.append(i + 1);
                }

                finish_callback();
            });

            thread.Start();
        }

        private void cancel_pb_Click_2(object sender, RoutedEventArgs e) {

            //Instead of using thread.Abort(); we use abort_thread_flag beacuse its safer for many reasons
            abort_thread = true;

            pb_2.Value = 0;

            start_pb_2.IsEnabled = true;
            cancel_pb_2.IsEnabled = false;

            start_pb.IsEnabled = true;

            pb_text.Foreground = Brushes.Red;
            pb_text.Text = "Job Cancelled...";
        }

        private void onFinish() {

            sc.Post(new SendOrPostCallback(o =>
            {
                pb_2.Value = 100;

                start_pb_2.IsEnabled = true;
                cancel_pb_2.IsEnabled = false;

                start_pb.IsEnabled = true;

                pb_text.Foreground = Brushes.Green;
                pb_text.Text = "Job Completed!";

            }), null);

            MessageBox.Show(Define.PATH + "\\" + Define.FILENAME + "\n\n Watch ticks: " + stopwatch.ElapsedTicks, "Synchronization Context Message", MessageBoxButton.OK, MessageBoxImage.Information);
            
            openFile();
        }

        private void openFile() {

            using (Process myProcess = new Process())
            {
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.FileName = Define.PATH + "\\" + Define.FILENAME;

                myProcess.Start();
            }
        }
    }
}
